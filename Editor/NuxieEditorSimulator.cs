using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nuxie.Unity.Internal;
namespace Nuxie.Unity.Editor
{
    /// <summary>Explicit, in-memory simulation. Never connects to Nuxie or a store.</summary>
    public sealed class NuxieEditorSimulator : IDisposable
    {
        private readonly Simulation transport = new Simulation();
        public INuxieClient Client { get; }
        public NuxieEditorSimulator() { Client = new Client(transport); }
        public void EmitAppAction(string name) => transport.Action(name);
        public void Dispose() => ((Client)Client).Detach();
        private sealed class Simulation : ITransport
        {
            private string session;
            private string customer;
            private string anonymous = Guid.NewGuid().ToString("N");
            private ulong generation;
            private ulong revision;
            private readonly Dictionary<string,double> balances = new Dictionary<string,double>();
            private readonly Dictionary<string,JObject> receipts = new Dictionary<string,JObject>();
            public string Platform => "ios";
            public event Action<string> Event;
            public void CheckThread() { }
            public void Dispose() { Event = null; }
            private string Key(string feature,string entity) => (customer ?? anonymous) + ":" + feature + ":" + entity;
            private double Balance(string feature,string entity) { var key = Key(feature,entity); return balances.TryGetValue(key,out var n) ? n : entity != null && entity.StartsWith("unknown-",StringComparison.Ordinal) ? 0 : 100; }
            private JObject Access(double balance) => new JObject { ["allowed"] = balance > 0, ["unlimited"] = false, ["balance"] = balance, ["type"] = "metered" };
            private JObject Snapshot() => new JObject { ["identityGeneration"] = generation.ToString(), ["revision"] = (++revision).ToString(), ["state"] = "ready", ["all"] = new JObject { ["energy"] = Access(Balance("energy",null)) } };
            private void Emit(string name,JObject payload) => Event?.Invoke(Wire.Encode(new JObject { ["session"] = session, ["name"] = name, ["payload"] = payload }));
            public void Action(string name) => Emit("appAction",new JObject { ["name"] = name, ["payload"] = new JObject { ["simulated"] = true }, ["experience"] = new JObject { ["experienceId"] = "editor-simulation" } });
            public Task<string> Invoke(string method,JObject args)
            {
                JObject result = null;
                switch(method)
                {
                    case "configure": var config = Wire.Object((string)args["configuration"]); session = (string)config["session"]; result = new JObject { ["contract"] = 1,["session"] = session,["nativeVersion"] = "SIMULATED",["snapshot"] = Snapshot() }; break;
                    case "shutdown": session = null; break;
                    case "identify": customer = (string)args["customerId"]; generation++; result = Snapshot(); Emit("features",result); break;
                    case "reset": customer = null; anonymous = Guid.NewGuid().ToString("N"); generation++; result = Snapshot(); Emit("features",result); break;
                    case "getIdentity": result = new JObject { ["distinctId"] = customer ?? anonymous,["anonymousId"] = anonymous,["isIdentified"] = customer != null }; break;
                    case "hasFeature": var q = Wire.Object((string)args["options"]); result = Access(Balance((string)args["featureId"],(string)q["entityId"])); result["allowed"] = (double)result["balance"] >= (double)q["requiredBalance"]; break;
                    case "consumeFeature":
                        var command = Wire.Object((string)args["options"]); var feature = (string)args["featureId"]; var entity = (string)command["entityId"]; var id = (string)command["operationId"]; var quantity = (double)command["quantity"];
                        var key = Key(feature,entity); var receiptKey = (customer ?? anonymous) + ":" + id;
                        if (receipts.TryGetValue(receiptKey,out var previous)) {
                            if ((string)previous["featureId"] != feature || (double)previous["quantity"] != quantity || (string)previous["entityId"] != entity) throw new NuxieException("operationConflict","Simulated operation ID conflict");
                            result = (JObject)previous.DeepClone(); result["idempotentReplay"] = true; break;
                        }
                        var before = Balance(feature,entity); var accepted = before >= quantity; var after = accepted ? before - quantity : before; balances[key] = after;
                        result = new JObject { ["customerId"] = customer ?? anonymous, ["featureId"] = feature, ["entityId"] = entity, ["operationId"] = id, ["quantity"] = quantity, ["accepted"] = accepted, ["code"] = accepted ? "consumed" : "insufficient", ["balance"] = after,["active"] = after > 0,["unlimited"] = false,["idempotentReplay"] = false,["occurredAtMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                        receipts[receiptKey] = (JObject)result.DeepClone(); Emit("features",Snapshot()); break;
                    case "restorePurchases": result = new JObject { ["type"] = "noPurchases" }; break;
                    case "trigger": Emit("activity",new JObject { ["schemaVersion"] = 1,["id"] = Guid.NewGuid().ToString(),["name"] = "simulated_trigger",["timestampMs"] = 0,["receivedAtMs"] = 0,["properties"] = new JObject { ["event"] = (string)args["event"] } }); break;
                }
                return Task.FromResult(result == null ? null : Wire.Encode(result));
            }
        }
    }
}
