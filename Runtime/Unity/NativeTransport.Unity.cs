#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Scripting;
namespace Nuxie.Unity.Internal
{
    internal sealed partial class NativeTransport
    {
        private NativeDispatcher dispatcher;
        private bool attached;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject bridge;
        private AndroidCallback callback;
#endif
        public string Platform
        {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return "ios";
#elif UNITY_ANDROID && !UNITY_EDITOR
                return "android";
#else
                return "editor";
#endif
            }
        }
        public void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != NativeDispatcher.MainThreadId) throw new NuxieException("wrongThread","Call Nuxie from Unity's main thread");
        }
        private void Send(string request)
        {
            if (Platform == "editor") throw new NuxieException("unsupportedPlatform","Editor calls require the explicitly selected simulator");
            if (!attached)
            {
                dispatcher = NativeDispatcher.Create(Receive);
                try
                {
#if UNITY_IOS && !UNITY_EDITOR
                NativeDispatcher.IosReceiver = dispatcher.Enqueue;
                nuxie_unity_attach(IosCallbackRoot);
#elif UNITY_ANDROID && !UNITY_EDITOR
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null) throw new NuxieException("missingActivity","No current Android Activity");
                    callback = new AndroidCallback(dispatcher.Enqueue);
                    bridge = new AndroidJavaObject("ai.nuxie.unity.NuxieUnityBridge",activity,callback);
                }
#endif
                    attached = true;
                }
                catch
                {
                    if (dispatcher != null) { dispatcher.Stop(); UnityEngine.Object.Destroy(dispatcher.gameObject); }
                    NativeDispatcher.IosReceiver = null;
                    throw;
                }
            }
#if UNITY_IOS && !UNITY_EDITOR
            nuxie_unity_dispatch(request);
#elif UNITY_ANDROID && !UNITY_EDITOR
            bridge.Call("dispatch",request);
#endif
        }
        private void DetachNative()
        {
            if (!attached) return;
#if UNITY_IOS && !UNITY_EDITOR
            nuxie_unity_detach(); NativeDispatcher.IosReceiver = null;
#elif UNITY_ANDROID && !UNITY_EDITOR
            bridge?.Call("invalidate"); bridge?.Dispose(); bridge = null; callback = null;
#endif
            if (dispatcher != null) { dispatcher.Stop(); UnityEngine.Object.Destroy(dispatcher.gameObject); }
            attached = false;
        }
#if UNITY_IOS && !UNITY_EDITOR
        private delegate void NativeCallback(IntPtr bytes);
        private static readonly NativeCallback IosCallbackRoot = IosCallback;
        [AOT.MonoPInvokeCallback(typeof(NativeCallback))]
        private static void IosCallback(IntPtr bytes) => NativeDispatcher.IosReceiver?.Invoke(Marshal.PtrToStringUTF8(bytes));
        [DllImport("__Internal")] private static extern void nuxie_unity_attach(NativeCallback callback);
        [DllImport("__Internal")] private static extern void nuxie_unity_dispatch(string request);
        [DllImport("__Internal")] private static extern void nuxie_unity_detach();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        [Preserve]
        private sealed class AndroidCallback : AndroidJavaProxy
        {
            private readonly Action<string> receive;
            internal AndroidCallback(Action<string> receive) : base("ai.nuxie.unity.NuxieUnityBridge$Callback") { this.receive = receive; }
            [Preserve] public void onMessage(string message) => receive(message);
        }
#endif
    }
    [Preserve]
    internal sealed class NativeDispatcher : MonoBehaviour
    {
        internal static int MainThreadId;
        internal static Action<string> IosReceiver;
        private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private Action<string> receiver;
        private bool previousRunInBackground;
        private volatile bool stopped;
        internal static NativeDispatcher Create(Action<string> receiver)
        {
            var host = new GameObject("Nuxie Dispatcher") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(host); var dispatcher = host.AddComponent<NativeDispatcher>();
            dispatcher.previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            dispatcher.receiver = receiver; return dispatcher;
        }
        internal void Enqueue(string message) { if (!stopped) queue.Enqueue(message); }
        internal void Stop()
        {
            if (stopped) return;
            stopped = true; receiver = null;
            Application.runInBackground = previousRunInBackground;
            while (queue.TryDequeue(out _)) { }
        }
        private void Update() { for (int i = 0; i < 128 && queue.TryDequeue(out var message); i++) receiver?.Invoke(message); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { MainThreadId = Thread.CurrentThread.ManagedThreadId; Nuxie.ResetRuntime(); IosReceiver = null; }
        private void OnApplicationQuit() => Nuxie.ResetRuntime();
    }
}
#endif
