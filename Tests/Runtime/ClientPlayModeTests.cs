using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Nuxie.Unity.Tests
{
    public sealed class ClientPlayModeTests
    {
        [UnityTest] public IEnumerator SceneObjectsDoNotOwnTheClient()
        {
            var before = Nuxie.Client;
            var host = new GameObject("temporary-scene-object"); Object.Destroy(host);
            yield return null;
            Assert.AreSame(before,Nuxie.Client);
            Assert.AreEqual(NuxieStatusKind.Unconfigured,before.Status.State);
        }
        [UnityTest] public IEnumerator NativeCallbacksDrainOnUnityThreadAndRestoreBackgroundSetting()
        {
            var previous = Application.runInBackground;
            Application.runInBackground = false;
            var thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int deliveredThread = 0;
            var dispatcher = global::Nuxie.Unity.Internal.NativeDispatcher.Create(_ => deliveredThread = System.Threading.Thread.CurrentThread.ManagedThreadId);
            try
            {
                Assert.IsTrue(Application.runInBackground);
                var enqueue = System.Threading.Tasks.Task.Run(() => dispatcher.Enqueue("callback"));
                var deadline = Time.realtimeSinceStartup + 3;
                while ((deliveredThread == 0 || !enqueue.IsCompleted) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(enqueue.IsCompleted);
                Assert.AreEqual(thread,deliveredThread);
                dispatcher.Stop();
                Assert.IsFalse(Application.runInBackground);
                deliveredThread = 0; dispatcher.Enqueue("late"); yield return null;
                Assert.AreEqual(0,deliveredThread);
            }
            finally { dispatcher.Stop(); Object.Destroy(dispatcher.gameObject); Application.runInBackground = previous; }
        }
        [TestCase("unknown",false)] [TestCase("unknown",true)]
        [TestCase("allowed",false)] [TestCase("allowed",true)]
        [TestCase("denied",false)] [TestCase("denied",true)]
        public void GateStopsObservingWhenItsInitialEventDisablesIt(string initial,bool disableObject)
        {
            Nuxie.ResetRuntime();
            var transport = new GateTransport(initial);
            var client = new global::Nuxie.Unity.Internal.Client(transport);
            typeof(Nuxie).GetField("client",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null,client);
            var setup = client.ConfigureAsync(new NuxieOptions { IosApiKey = "test" });
            Assert.IsTrue(setup.IsCompleted); setup.GetAwaiter().GetResult();
            var host = new GameObject("feature-gate-test"); host.SetActive(false);
            var gate = host.AddComponent<NuxieFeatureGate>();
            JsonUtility.FromJsonOverwrite("{\"featureId\":\"energy\"}",gate);
            int events = 0;
            UnityEngine.Events.UnityAction disable = () => { events++; if (disableObject) host.SetActive(false); else gate.enabled = false; };
            gate.Unknown.AddListener(disable); gate.Allowed.AddListener(disable); gate.Denied.AddListener(disable);
            try
            {
                host.SetActive(true);
                Assert.AreEqual(1,events); Assert.IsFalse(gate.isActiveAndEnabled);
                transport.Emit(initial == "allowed" ? "denied" : "allowed");
                Assert.AreEqual(1,events,"A disabled gate must not receive subsequent Feature changes");
            }
            finally { Object.DestroyImmediate(host); Nuxie.ResetRuntime(); }
        }
        private sealed class GateTransport : global::Nuxie.Unity.Internal.ITransport
        {
            private readonly string initial;
            private string session;
            internal GateTransport(string initial) { this.initial = initial; }
            public string Platform => "ios";
            public event System.Action<string> Event;
            public void CheckThread() { }
            public void Dispose() { Event = null; }
            private Newtonsoft.Json.Linq.JObject Snapshot(string state,int revision) => new Newtonsoft.Json.Linq.JObject {
                ["state"] = state == "unknown" ? "unknown" : "ready", ["identityGeneration"] = "0", ["revision"] = revision.ToString(),
                ["all"] = new Newtonsoft.Json.Linq.JObject { ["energy"] = new Newtonsoft.Json.Linq.JObject {
                    ["allowed"] = state == "allowed", ["unlimited"] = false, ["balance"] = state == "allowed" ? 1 : 0, ["type"] = "metered"
                } }
            };
            public System.Threading.Tasks.Task<string> Invoke(string method,Newtonsoft.Json.Linq.JObject arguments)
            {
                session = (string)Newtonsoft.Json.Linq.JObject.Parse((string)arguments["configuration"])["session"];
                return System.Threading.Tasks.Task.FromResult(new Newtonsoft.Json.Linq.JObject {
                    ["contract"] = 1, ["session"] = session, ["nativeVersion"] = "test", ["snapshot"] = Snapshot(initial,0)
                }.ToString());
            }
            internal void Emit(string state) => Event?.Invoke(new Newtonsoft.Json.Linq.JObject {
                ["session"] = session, ["name"] = "features", ["payload"] = Snapshot(state,1)
            }.ToString());
        }
        [Test] public void SnapshotsDefensivelyCopyInput()
        {
            var source = new System.Collections.Generic.Dictionary<string,FeatureAccess>();
            var snapshot = new FeatureSnapshot(FeatureStateKind.Ready,1,1,source);
            source.Add("energy",new FeatureAccess(true,false,1,FeatureType.Metered));
            Assert.IsEmpty(snapshot.All);
        }
    }
}
