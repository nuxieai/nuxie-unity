using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nuxie.Unity;
using Nuxie.Unity.Internal;
using Xunit;
namespace Nuxie.Unity.Tests
{
    internal sealed class FakeTransport : ITransport
    {
        internal string Session;
        internal JObject Configuration;
        public string Platform => "ios";
        public event Action<string> Event;
        internal readonly List<string> Calls = new List<string>();
        internal Func<string,JObject,Task<string>> Handler;
        public void CheckThread() { }
        public void Dispose() { }
        internal static JObject Snapshot(string revision = "0", string generation = "0", string state = "unknown") => new JObject { ["revision"] = revision, ["identityGeneration"] = generation, ["state"] = state, ["all"] = new JObject() };
        internal void Emit(string name,JObject payload,string session = null) => Event?.Invoke(Wire.Encode(new JObject { ["name"] = name, ["payload"] = payload, ["session"] = session ?? Session }));
        public Task<string> Invoke(string method,JObject args)
        {
            Calls.Add(method);
            if (method == "configure") { Configuration = Wire.Object((string)args["configuration"]); Session = (string)Configuration["session"]; }
            if (Handler != null) return Handler(method,args);
            return Task.FromResult(method == "configure" ? Wire.Encode(new JObject { ["contract"] = 1, ["session"] = Session, ["nativeVersion"] = "test", ["snapshot"] = Snapshot() }) : method == "identify" || method == "reset" ? Wire.Encode(Snapshot("1","1","ready")) : null);
        }
    }
    public sealed class ClientTests
    {
        private static NuxieOptions Options() => new NuxieOptions { IosApiKey = "public-ios", AndroidApiKey = "public-android" };
        [Fact] public async Task SetupSharesPendingTaskAndCopiesOptions()
        {
            var t = new FakeTransport(); var completion = new TaskCompletionSource<string>(); t.Handler = (m,a) => completion.Task;
            var c = new Client(t); var options = Options(); var first = c.ConfigureAsync(options);
            Assert.Same(first,c.ConfigureAsync(Options())); options.IosApiKey = "changed";
            await Assert.ThrowsAsync<NuxieException>(() => c.ConfigureAsync(options));
            completion.SetResult(Wire.Encode(new JObject { ["contract"] = 1,["session"] = t.Session,["nativeVersion"] = "test",["snapshot"] = FakeTransport.Snapshot() }));
            await first; Assert.Equal(NuxieStatusKind.Configured,c.Status.State); Assert.Single(t.Calls);
        }
        [Fact] public async Task SetupObserverCanReenterAndFailureCanRetry()
        {
            var t = new FakeTransport(); var c = new Client(t); Task nested = null;
            using var observer = c.SubscribeStatus(s => { if (s.State == NuxieStatusKind.Configuring) nested = c.ConfigureAsync(Options()); });
            t.Handler = (m,a) => Task.FromException<string>(new Exception("offline"));
            await Assert.ThrowsAsync<NuxieException>(() => c.ConfigureAsync(Options())); Assert.NotNull(nested);
            t.Handler = null; await c.ConfigureAsync(Options()); Assert.Equal(NuxieStatusKind.Configured,c.Status.State);
        }
        [Fact] public async Task SnapshotsPreservePrecisionAndIgnoreOldSessions()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            t.Emit("features",FakeTransport.Snapshot("9007199254740993","1","ready"));
            t.Emit("features",FakeTransport.Snapshot("9007199254740992","1"));
            t.Emit("features",FakeTransport.Snapshot("9007199254740994","1"),"dead");
            Assert.Equal(9007199254740993UL,c.Features.Revision); Assert.Equal(FeatureStateKind.Ready,c.Features.State);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string,FeatureAccess>)c.Features.All).Add("bad",null));
        }
        [Fact] public async Task ScopedReadCannotCrossReset()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            var pending = new TaskCompletionSource<string>(); t.Handler = (m,a) => m == "hasFeature" ? pending.Task : Task.FromResult(Wire.Encode(FakeTransport.Snapshot("1","1","ready")));
            var read = c.HasFeatureAsync("energy",new FeatureQuery { EntityId = "a" }); await c.ResetAsync();
            pending.SetResult("{\"allowed\":true,\"unlimited\":false,\"balance\":1,\"type\":\"metered\"}");
            var error = await Assert.ThrowsAsync<NuxieException>(() => read); Assert.Equal("staleOperation",error.Code);
        }
        [Fact] public async Task ShutdownSettlesPendingReadsBeforeNativeShutdownCompletes()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            var pending = new TaskCompletionSource<string>(); var shutdown = new TaskCompletionSource<string>();
            t.Handler = (m,a) => m == "shutdown" ? shutdown.Task : pending.Task;
            var read = c.HasFeatureAsync("energy"); var stop = c.ShutdownAsync();
            var completed = await Task.WhenAny(read,Task.Delay(1000));
            Assert.Same(read,completed);
            var error = await Assert.ThrowsAsync<NuxieException>(() => read); Assert.Equal("sessionExpired",error.Code);
            shutdown.SetResult(null); await stop;
            Assert.Equal(NuxieStatusKind.Unconfigured,c.Status.State);
            t.Handler = null; await c.ConfigureAsync(Options());
            Assert.Equal(NuxieStatusKind.Configured,c.Status.State);
        }
        [Fact] public async Task NewSessionAcceptsItsOwnGenerationSequence()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            t.Emit("features",FakeTransport.Snapshot("20","9","ready"));
            await c.ShutdownAsync(); await c.ConfigureAsync(Options());
            t.Emit("features",FakeTransport.Snapshot("1","0","ready"));
            Assert.Equal(0UL,c.Features.IdentityGeneration);
            Assert.Equal(FeatureStateKind.Ready,c.Features.State);
        }
        private sealed class PurchaseController : INuxiePurchaseController
        {
            internal int Calls;
            internal readonly TaskCompletionSource<PurchaseResult> Result = new TaskCompletionSource<PurchaseResult>();
            public Task<PurchaseResult> PurchaseAsync(StoreProduct product) { Calls++; return Result.Task; }
            public Task<RestoreResult> RestorePurchasesAsync() => Task.FromResult(new RestoreResult(RestoreOutcome.NoPurchases));
        }
        [Theory] [InlineData(false,"full")] [InlineData(true,"observer")]
        public async Task CheckoutOwnerAlsoOwnsNativeTransactionFinishing(bool external,string mode)
        {
            var t = new FakeTransport(); var c = new Client(t); var options = Options();
            if (external) options.Billing = NuxieBilling.External(new PurchaseController());
            await c.ConfigureAsync(options);
            Assert.Equal(mode,(string)t.Configuration["purchaseHandlingMode"]);
            Assert.Equal(external,(bool)t.Configuration["externalBilling"]);
        }
        [Fact] public async Task DuplicatePurchaseCallbacksStartOnlyOneCheckout()
        {
            var t = new FakeTransport(); var c = new Client(t); var owner = new PurchaseController();
            var options = Options(); options.Billing = NuxieBilling.External(owner); await c.ConfigureAsync(options);
            var receipt = new TaskCompletionSource<JObject>();
            t.Handler = (m,a) => { if (m == "completePurchase") receipt.SetResult(Wire.Object((string)a["result"])); return Task.FromResult<string>(null); };
            var payload = JObject.Parse("{\"requestId\":\"checkout-1\",\"product\":{\"productId\":\"coins\"}}");
            t.Emit("purchase",payload); t.Emit("purchase",payload); Assert.Equal(1,owner.Calls);
            owner.Result.SetResult(new PurchaseResult(PurchaseOutcome.Pending));
            var completed = await Task.WhenAny(receipt.Task,Task.Delay(1000)); Assert.Same(receipt.Task,completed);
            Assert.Equal("pending",(string)(await receipt.Task)["type"]);
        }
        [Fact] public async Task CheckoutCompletionCannotEnterANewSession()
        {
            var t = new FakeTransport(); var c = new Client(t); var owner = new PurchaseController();
            var options = Options(); options.Billing = NuxieBilling.External(owner); await c.ConfigureAsync(options);
            t.Emit("purchase",JObject.Parse("{\"requestId\":\"checkout-1\",\"product\":{\"productId\":\"coins\"}}"));
            await c.ShutdownAsync(); await c.ConfigureAsync(Options());
            owner.Result.SetResult(new PurchaseResult(PurchaseOutcome.Purchased));
            await Task.Yield();
            Assert.DoesNotContain("completePurchase",t.Calls);
        }
        [Fact] public async Task DetachedRuntimeCannotDispatchThroughAnOldClientReference()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            c.Detach(); await c.ShutdownAsync();
            Assert.Equal(NuxieStatusKind.Unconfigured,c.Status.State);
            await Assert.ThrowsAsync<NuxieException>(() => c.TriggerAsync("late"));
            await Assert.ThrowsAsync<NuxieException>(() => c.ConfigureAsync(Options()));
            Assert.Single(t.Calls);
        }
        [Fact] public async Task IdentityTransitionRetainsSnapshotsDeliveredBeforeItsCompletion()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            t.Handler = (m,a) =>
            {
                t.Emit("features",FakeTransport.Snapshot("2","1","ready"));
                return Task.FromResult(Wire.Encode(FakeTransport.Snapshot("1","1","unknown")));
            };
            await c.IdentifyAsync("player");
            Assert.Equal(FeatureStateKind.Ready,c.Features.State);
            Assert.Equal(2UL,c.Features.Revision);
        }
        [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(0.5)] [InlineData(9007199254740992d)]
        public async Task InvalidQuantityNeverReachesNative(double quantity)
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            await Assert.ThrowsAsync<NuxieException>(() => c.ConsumeFeatureAsync("energy",new FeatureCommand { Quantity = quantity, OperationId = "op" })); Assert.Single(t.Calls);
        }
        [Fact] public async Task LastUnitReceiptPreservesOriginalDecision()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options());
            t.Handler = (m,a) => Task.FromResult("{\"customerId\":\"player\",\"featureId\":\"energy\",\"operationId\":\"turn-7\",\"quantity\":1,\"accepted\":true,\"balance\":0,\"active\":false,\"unlimited\":false,\"idempotentReplay\":true,\"occurredAtMs\":1789320000000,\"code\":\"consumed\"}");
            var r = await c.ConsumeFeatureAsync("energy",new FeatureCommand { OperationId = "turn-7" });
            Assert.True(r.Accepted); Assert.False(r.Active); Assert.True(r.IdempotentReplay); Assert.Equal(1789320000000,r.OccurredAtMs); Assert.Equal("player",r.CustomerId);
        }
        [Fact] public async Task FeatureObserversIgnoreUnrelatedUpdatesAndDispose()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options()); int notifications = 0;
            var d = c.ObserveFeature("energy",s => notifications++);
            t.Emit("features",FakeTransport.Snapshot("1","0","ready")); t.Emit("features",FakeTransport.Snapshot("2","0","ready"));
            Assert.Equal(2,notifications); d.Dispose(); t.Emit("features",FakeTransport.Snapshot("3","0")); Assert.Equal(2,notifications);
        }
        [Fact] public async Task ActionPayloadIsImmutableAndObserversAreIsolated()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options()); int errors = 0; AppAction action = null;
            c.SubscribeErrors(e => errors++); c.SubscribeAppAction(a => throw new Exception("listener")); c.SubscribeAppAction(a => action = a);
            t.Emit("appAction",JObject.Parse("{\"name\":\"continue\",\"payload\":{\"source\":\"lab\"},\"experience\":{\"experienceId\":\"exp\"}}"));
            Assert.Equal(1,errors); Assert.Equal("continue",action.Name); Assert.Equal("lab",action.Payload["source"]);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string,object>)action.Payload).Add("bad",true));
        }
        [Fact] public async Task CyclicPropertiesFailBeforeDispatch()
        {
            var t = new FakeTransport(); var c = new Client(t); await c.ConfigureAsync(Options()); var props = new Dictionary<string,object>(); props["cycle"] = props;
            await Assert.ThrowsAsync<NuxieException>(() => c.TriggerAsync("event",props)); Assert.Single(t.Calls);
        }
    }
}
