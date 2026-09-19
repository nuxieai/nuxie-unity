using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Nuxie.Unity.Example.Tests
{
    public sealed class SceneTests
    {
        [Test] public void NativeScreenStackRestoresTheExistingGamePauseState()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SdkLab.unity");
            var lab = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MonoBehaviour>()).First(b => b.GetType().Name == "SdkLab");
            var handle = lab.GetType().GetMethod("HandleScreenActivity",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var oldScale = Time.timeScale; var oldAudio = AudioListener.pause;
            var props = new System.Collections.Generic.Dictionary<string,object> { ["screen_id"] = "offer", ["journey_id"] = "run" };
            try
            {
                Time.timeScale = 0.5f; AudioListener.pause = false;
                handle.Invoke(lab,new object[] { "screen_shown",props });
                Assert.AreEqual(0,Time.timeScale); Assert.IsTrue(AudioListener.pause);
                props["screen_id"] = "details"; handle.Invoke(lab,new object[] { "screen_shown",props });
                props["revealing_screen_id"] = "offer"; handle.Invoke(lab,new object[] { "screen_dismissed",props });
                Assert.AreEqual(0,Time.timeScale);
                props.Remove("revealing_screen_id"); props["screen_id"] = "offer"; handle.Invoke(lab,new object[] { "screen_dismissed",props });
                Assert.AreEqual(0.5f,Time.timeScale); Assert.IsFalse(AudioListener.pause);
            }
            finally { Time.timeScale = oldScale; AudioListener.pause = oldAudio; }
        }
        [Test] public void CompletedJourneyReleasesOnlyItsOwnedGamePause()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SdkLab.unity");
            var lab = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MonoBehaviour>()).First(b => b.GetType().Name == "SdkLab");
            var handle = lab.GetType().GetMethod("HandleScreenActivity",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var oldScale = Time.timeScale; var oldAudio = AudioListener.pause;
            var first = new System.Collections.Generic.Dictionary<string,object> { ["screen_id"] = "offer", ["journey_id"] = "first" };
            var second = new System.Collections.Generic.Dictionary<string,object> { ["screen_id"] = "offer", ["journey_id"] = "second" };
            try
            {
                Time.timeScale = .75f; AudioListener.pause = false;
                handle.Invoke(lab,new object[] { "screen_shown",first });
                handle.Invoke(lab,new object[] { "screen_shown",second });
                first.Remove("screen_id"); second.Remove("screen_id");
                handle.Invoke(lab,new object[] { "journey_completed",first });
                Assert.AreEqual(0,Time.timeScale); Assert.IsTrue(AudioListener.pause);
                handle.Invoke(lab,new object[] { "journey_completed",second });
                Assert.AreEqual(.75f,Time.timeScale); Assert.IsFalse(AudioListener.pause);
            }
            finally { Time.timeScale = oldScale; AudioListener.pause = oldAudio; }
        }
        [TestCase("SdkLab")]
        [TestCase("Gameplay")]
        public void SceneContainsWorkingLabController(string name)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            Assert.That(objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount), Is.Zero);
            Assert.That(objects.SelectMany(o => o.GetComponents<MonoBehaviour>()).Any(b => b != null && b.GetType().FullName == "Nuxie.Unity.Samples.SdkLab"), Is.True);
        }
    }
}
