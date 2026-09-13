using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace Nuxie.Unity.Internal
{
    internal interface ITransport : IDisposable
    {
        string Platform { get; }
        event Action<string> Event;
        void CheckThread();
        Task<string> Invoke(string method, JObject arguments);
    }
    internal sealed class Observers<T>
    {
        private readonly List<Action<T>> items = new List<Action<T>>();
        private readonly Action<Exception> report;
        internal Observers(Action<Exception> report) { this.report = report; }
        internal IDisposable Add(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            items.Add(action); return new Subscription(() => items.Remove(action));
        }
        internal void Call(Action<T> action, T value) { try { action(value); } catch (Exception e) { report(e); } }
        internal void Send(T value) { foreach (var action in items.ToArray()) Call(action, value); }
        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            internal Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() { var action = dispose; dispose = null; action?.Invoke(); }
        }
    }
    internal sealed class Client : INuxieClient
    {
        private readonly ITransport transport;
        private readonly Observers<NuxieStatus> statuses;
        private readonly Observers<FeatureSnapshot> features;
        private readonly Observers<NuxieActivity> activities;
        private readonly Observers<AppAction> actions;
        private readonly Observers<NuxieException> errors;
        private readonly HashSet<string> purchases = new HashSet<string>();
        private Task setup;
        private Task shutdown;
        private string configurationKey;
        private string session;
        private TaskCompletionSource<bool> sessionEnded;
        private long identityEpoch;
        private bool changingIdentity;
        private bool detached;
        private FeatureSnapshot pendingIdentitySnapshot;
        private INuxiePurchaseController controller;
        public NuxieStatus Status { get; private set; } = new NuxieStatus(NuxieStatusKind.Unconfigured);
        public FeatureSnapshot Features { get; private set; } = FeatureSnapshot.Empty();
        internal Client(ITransport transport)
        {
            this.transport = transport;
            errors = new Observers<NuxieException>(_ => { });
            statuses = new Observers<NuxieStatus>(Report); features = new Observers<FeatureSnapshot>(Report);
            activities = new Observers<NuxieActivity>(Report); actions = new Observers<AppAction>(Report);
        }
        private void Report(Exception e) => errors.Send(e as NuxieException ?? new NuxieException("observerError", e.Message, e));
        private void SetStatus(NuxieStatus value) { Status = value; statuses.Send(value); }
        private void SetFeatures(FeatureSnapshot value) { Features = value; features.Send(value); }
        public Task ConfigureAsync(NuxieOptions options)
        {
            transport.CheckThread();
            if (detached) throw new NuxieException("runtimeDetached","Get a new Nuxie.Client after the Unity runtime resets");
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (shutdown != null) throw new NuxieException("shuttingDown", "Wait for shutdown before configuring");
            if (transport.Platform != "ios" && transport.Platform != "android") throw new NuxieException("unsupportedPlatform", "Use a mobile player or explicitly select the Editor simulator");
            var billing = options.Billing ?? throw Wire.Invalid("Billing must be specified");
            var input = new JObject { ["contract"] = 1, ["apiKey"] = Wire.Text(transport.Platform == "ios" ? options.IosApiKey : options.AndroidApiKey,"Platform API key"),
                ["environment"] = options.Environment.ToString().ToLowerInvariant(), ["logLevel"] = options.LogLevel.ToString().ToLowerInvariant(),
                ["localeIdentifier"] = options.LocaleIdentifier, ["externalBilling"] = billing.Controller != null };
            if (!Enum.IsDefined(typeof(NuxieEnvironment), options.Environment) || !Enum.IsDefined(typeof(NuxieLogLevel),options.LogLevel)) throw Wire.Invalid("Invalid configuration enum");
            var key = Wire.Encode(input);
            if (setup != null || Status.State == NuxieStatusKind.Configured)
            {
                if (key != configurationKey || !ReferenceEquals(controller, billing.Controller)) throw new NuxieException("alreadyConfigured", "Shutdown before changing configuration");
                return setup ?? Task.CompletedTask;
            }
            configurationKey = key; controller = billing.Controller;
            sessionEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            session = Guid.NewGuid().ToString("N"); input["session"] = session;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            setup = completion.Task;
            identityEpoch++; SetFeatures(FeatureSnapshot.Empty());
            transport.Event += OnEvent;
            SetStatus(new NuxieStatus(NuxieStatusKind.Configuring));
            _ = ConfigureCore(input, completion);
            return completion.Task;
        }
        private async Task ConfigureCore(JObject input, TaskCompletionSource<bool> completion)
        {
            try
            {
                var result = Wire.Object(await transport.Invoke("configure", new JObject { ["configuration"] = Wire.Encode(input) }));
                if ((int?)result["contract"] != 1 || (string)result["session"] != session) throw new NuxieException("incompatibleBridge", "Native bridge does not match the package");
                AcceptSnapshot(Wire.Snapshot((JObject)result["snapshot"]));
                SetStatus(new NuxieStatus(NuxieStatusKind.Configured, Wire.String(result,"nativeVersion")));
                completion.TrySetResult(true);
            }
            catch (Exception e)
            {
                sessionEnded?.TrySetResult(true);
                transport.Event -= OnEvent; transport.Dispose(); session = null; controller = null; configurationKey = null;
                var error = e as NuxieException ?? new NuxieException("nativeError", e.Message, e);
                SetStatus(new NuxieStatus(NuxieStatusKind.Failed, error: error)); completion.TrySetException(error);
            }
            finally { setup = null; }
        }
        private async Task<JObject> Invoke(string method, JObject args = null, bool identitySensitive = false)
        {
            transport.CheckThread();
            EnsureConfigured();
            if (identitySensitive && changingIdentity) throw new NuxieException("identityBusy","Wait for the current identity transition");
            var attached = session; var epoch = identityEpoch; args = args ?? new JObject(); args["session"] = attached;
            var ended = sessionEnded.Task;
            var operation = transport.Invoke(method, args);
            if (await Task.WhenAny(operation,ended) == ended)
            {
                _ = ObserveAbandoned(operation);
                throw new NuxieException("sessionExpired","The native session ended");
            }
            var result = await operation;
            if (session != attached || (identitySensitive && epoch != identityEpoch)) throw new NuxieException("staleOperation", "The native session or customer changed");
            return result == null ? new JObject() : Wire.Object(result);
        }
        private static async Task ObserveAbandoned(Task operation) { try { await operation; } catch { } }
        private void InvalidateIdentity()
        {
            identityEpoch++;
            SetFeatures(FeatureSnapshot.Empty(Features.IdentityGeneration));
        }
        public async Task IdentifyAsync(string customerId, IdentityOptions options = null)
        {
            transport.CheckThread(); Wire.Text(customerId,"customerId");
            var properties = new JObject { ["properties"] = Wire.Properties(options?.Properties), ["propertiesSetOnce"] = Wire.Properties(options?.PropertiesSetOnce) };
            await ChangeIdentity("identify", new JObject { ["customerId"] = customerId, ["properties"] = Wire.Encode(properties) });
        }
        private void EnsureConfigured() { if (detached) throw new NuxieException("runtimeDetached","The Unity runtime detached"); if (Status.State != NuxieStatusKind.Configured || shutdown != null) throw new NuxieException("notConfigured","Await ConfigureAsync first"); }
        public Task ResetAsync() => ChangeIdentity("reset",new JObject { ["keepAnonymousId"] = false });
        private async Task ChangeIdentity(string method, JObject args)
        {
            transport.CheckThread(); EnsureConfigured();
            if (changingIdentity) throw new NuxieException("identityBusy","Wait for the current identity transition");
            changingIdentity = true; pendingIdentitySnapshot = null; InvalidateIdentity();
            FeatureSnapshot snapshot;
            try { snapshot = Wire.Snapshot(await Invoke(method,args)); }
            catch { changingIdentity = false; pendingIdentitySnapshot = null; throw; }
            var buffered = pendingIdentitySnapshot;
            pendingIdentitySnapshot = null; changingIdentity = false;
            if (buffered != null && IsNewerOrEqual(buffered,snapshot)) snapshot = buffered;
            AcceptSnapshot(snapshot);
        }
        public async Task<Identity> GetIdentityAsync()
        { var v = await Invoke("getIdentity", identitySensitive:true); return new Identity(Wire.String(v,"distinctId"),Wire.String(v,"anonymousId"),Wire.Bool(v,"isIdentified")); }
        public async Task SetLocaleAsync(string locale) { if (locale != null) Wire.Text(locale,"locale"); await Invoke("setLocaleIdentifier",new JObject { ["locale"] = locale }); }
        public async Task TriggerAsync(string eventName, IReadOnlyDictionary<string, object> properties = null)
        { await Invoke("trigger",new JObject { ["event"] = Wire.Text(eventName,"event"), ["properties"] = Wire.Encode(Wire.Properties(properties)) }); }
        public async Task DismissAsync() { await Invoke("dismiss"); }
        public async Task<FeatureAccess> HasFeatureAsync(string featureId, FeatureQuery query = null)
        {
            query = query ?? new FeatureQuery();
            if (query.EntityId != null) Wire.Text(query.EntityId,"entityId");
            if (!Enum.IsDefined(typeof(FeatureQueryPolicy),query.Policy)) throw Wire.Invalid("Invalid query policy");
            var options = new JObject { ["entityId"] = query.EntityId, ["requiredBalance"] = Wire.Quantity(query.RequiredBalance), ["policy"] = query.Policy == FeatureQueryPolicy.Remote ? "remote" : "cacheFirst" };
            return Wire.Access(await Invoke("hasFeature",new JObject { ["featureId"] = Wire.Text(featureId,"featureId"), ["options"] = Wire.Encode(options) },true));
        }
        public async Task<FeatureConsumption> ConsumeFeatureAsync(string featureId, FeatureCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (command.EntityId != null) Wire.Text(command.EntityId,"entityId");
            var operationId = Wire.Text(command.OperationId,"operationId"); var quantity = Wire.Quantity(command.Quantity);
            var options = new JObject { ["entityId"] = command.EntityId, ["operationId"] = operationId, ["quantity"] = quantity };
            var result = Wire.Receipt(await Invoke("consumeFeature",new JObject { ["featureId"] = Wire.Text(featureId,"featureId"), ["options"] = Wire.Encode(options) },true));
            if (result.OperationId != operationId || result.FeatureId != featureId || result.Quantity != quantity) throw new NuxieException("invalidReceipt","Native receipt does not match the command");
            return result;
        }
        public async Task<RestoreResult> RestorePurchasesAsync()
        { var v = await Invoke("restorePurchases",identitySensitive:true); return new RestoreResult(Wire.EnumValue<RestoreOutcome>(v,"type"),(string)v["message"]); }
        public Task ShutdownAsync()
        {
            transport.CheckThread();
            if (detached) return Task.CompletedTask;
            if (shutdown != null) return shutdown;
            if (setup != null) return ShutdownAfterSetup();
            if (Status.State != NuxieStatusKind.Configured) return Task.CompletedTask;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            shutdown = completion.Task; sessionEnded.TrySetResult(true); _ = ShutdownCore(completion); return completion.Task;
        }
        private async Task ShutdownAfterSetup() { try { await setup; } catch { } await ShutdownAsync(); }
        private async Task ShutdownCore(TaskCompletionSource<bool> completion)
        {
            try { await transport.Invoke("shutdown",new JObject { ["session"] = session }); completion.TrySetResult(true); }
            catch (Exception e) { completion.TrySetException(e); }
            finally
            { transport.Event -= OnEvent; transport.Dispose(); session = null; setup = null; shutdown = null; controller = null; configurationKey = null; purchases.Clear(); InvalidateIdentity(); SetStatus(new NuxieStatus(NuxieStatusKind.Unconfigured)); }
        }
        internal void Detach()
        { detached = true; sessionEnded?.TrySetResult(true); transport.Event -= OnEvent; transport.Dispose(); session = null; purchases.Clear(); controller = null; InvalidateIdentity(); SetStatus(new NuxieStatus(NuxieStatusKind.Unconfigured)); }
        private static bool IsNewerOrEqual(FeatureSnapshot value,FeatureSnapshot previous) =>
            value.IdentityGeneration > previous.IdentityGeneration ||
            (value.IdentityGeneration == previous.IdentityGeneration && value.Revision >= previous.Revision);
        private void AcceptSnapshot(FeatureSnapshot value)
        {
            if (value.IdentityGeneration < Features.IdentityGeneration || (value.IdentityGeneration == Features.IdentityGeneration && value.Revision < Features.Revision)) return;
            if (value.IdentityGeneration != Features.IdentityGeneration) identityEpoch++;
            SetFeatures(value);
        }
        private void OnEvent(string message)
        {
            try
            {
                var v = Wire.Object(message);
                if ((string)v["session"] != session || session == null) return;
                var p = (JObject)v["payload"];
                switch (Wire.String(v,"name"))
                {
                    case "features":
                        var snapshot = Wire.Snapshot(p);
                        if (!changingIdentity) AcceptSnapshot(snapshot);
                        else if (pendingIdentitySnapshot == null || IsNewerOrEqual(snapshot,pendingIdentitySnapshot)) pendingIdentitySnapshot = snapshot;
                        break;
                    case "activity": activities.Send(new NuxieActivity(Wire.String(p,"id"),Wire.String(p,"name"),(int)p["schemaVersion"],Wire.Number(p,"timestampMs"),Wire.Number(p,"receivedAtMs"),Wire.Map(p["properties"]))); break;
                    case "appAction": var e = (JObject)p["experience"]; actions.Send(new AppAction(Wire.String(p,"name"),Wire.Map(p["payload"]),new ExperienceRef(Wire.String(e,"experienceId"),(string)e["experienceVersion"],(string)e["journeyId"]))); break;
                    case "purchase": case "restore": _ = CompleteCommerce(Wire.String(v,"name"),p); break;
                    case "error": Report(new NuxieException((string)p["code"] ?? "nativeError",(string)p["message"] ?? "Native operation failed")); break;
                }
            }
            catch (Exception e) { Report(e); }
        }
        private async Task CompleteCommerce(string name, JObject payload)
        {
            var attached = session; var id = Wire.String(payload,"requestId"); var key = name + ":" + id;
            if (!purchases.Add(key)) return;
            var owner = controller;
            JObject result;
            try
            {
                if (owner == null) throw new NuxieException("missingController","External billing controller is missing");
                if (name == "purchase")
                { var r = await owner.PurchaseAsync(new StoreProduct(Wire.Map(payload["product"]))); result = new JObject { ["type"] = r.Outcome.ToString().ToLowerInvariant(), ["message"] = r.Message }; }
                else
                { var r = await owner.RestorePurchasesAsync(); result = new JObject { ["type"] = r.Outcome == RestoreOutcome.NoPurchases ? "noPurchases" : r.Outcome.ToString().ToLowerInvariant(), ["message"] = r.Message }; }
            }
            catch { result = new JObject { ["type"] = "failed", ["message"] = "External purchase controller failed" }; }
            if (session != attached || shutdown != null) return;
            try { await Invoke(name == "purchase" ? "completePurchase" : "completeRestore",new JObject { ["requestId"] = id, ["result"] = Wire.Encode(result) }); }
            catch (Exception e) { Report(e); }
        }
        public IDisposable SubscribeStatus(Action<NuxieStatus> observer) { transport.CheckThread(); var d = statuses.Add(observer); statuses.Call(observer,Status); return d; }
        public IDisposable SubscribeFeatures(Action<FeatureSnapshot> observer) { transport.CheckThread(); var d = features.Add(observer); features.Call(observer,Features); return d; }
        public IDisposable ObserveFeature(string featureId, Action<FeatureState> observer)
        {
            Wire.Text(featureId,"featureId"); if (observer == null) throw new ArgumentNullException(nameof(observer));
            FeatureState previous = null; ulong generation = 0;
            return SubscribeFeatures(snapshot => {
                snapshot.All.TryGetValue(featureId,out var access);
                if (previous != null && generation == snapshot.IdentityGeneration && previous.State == snapshot.State && Equals(previous.Access,access)) return;
                previous = new FeatureState(snapshot.State,access); generation = snapshot.IdentityGeneration; observer(previous);
            });
        }
        public IDisposable SubscribeActivity(Action<NuxieActivity> observer) { transport.CheckThread(); return activities.Add(observer); }
        public IDisposable SubscribeAppAction(Action<AppAction> observer) { transport.CheckThread(); return actions.Add(observer); }
        public IDisposable SubscribeErrors(Action<NuxieException> observer) { transport.CheckThread(); return errors.Add(observer); }
    }
}
