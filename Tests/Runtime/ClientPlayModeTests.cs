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
        [Test] public void SnapshotsDefensivelyCopyInput()
        {
            var source = new System.Collections.Generic.Dictionary<string,FeatureAccess>();
            var snapshot = new FeatureSnapshot(FeatureStateKind.Ready,1,1,source);
            source.Add("energy",new FeatureAccess(true,false,1,FeatureType.Metered));
            Assert.IsEmpty(snapshot.All);
        }
    }
}
