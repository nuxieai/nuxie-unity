using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace Nuxie.Unity.Internal
{
    internal sealed partial class NativeTransport : ITransport
    {
        private readonly Dictionary<string,TaskCompletionSource<string>> pending = new Dictionary<string,TaskCompletionSource<string>>();
        public event Action<string> Event;
        public Task<string> Invoke(string method, JObject arguments)
        {
            CheckThread();
            var id = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Add(id,completion);
            try { Send(Wire.Encode(new JObject { ["requestId"] = id, ["method"] = method, ["arguments"] = arguments })); }
            catch (Exception e) { pending.Remove(id); completion.TrySetException(e); }
            return completion.Task;
        }
        internal void Receive(string message)
        {
            try
            {
                var envelope = Wire.Object(message);
                var id = (string)envelope["requestId"];
                if (id == null) { Event?.Invoke(message); return; }
                if (!pending.TryGetValue(id,out var completion)) return;
                pending.Remove(id);
                if (envelope["error"] is JObject error) completion.TrySetException(new NuxieException((string)error["code"] ?? "nativeError",(string)error["message"] ?? "Native request failed"));
                else completion.TrySetResult((string)envelope["result"]);
            }
            catch (Exception e) { Event?.Invoke(Wire.Encode(new JObject { ["name"] = "transportError", ["message"] = e.Message })); }
        }
        public void Dispose()
        {
            DetachNative();
            foreach (var completion in pending.Values) completion.TrySetException(new NuxieException("sessionExpired","Unity runtime detached"));
            pending.Clear(); Event = null;
        }
#if !UNITY_5_3_OR_NEWER
        public string Platform => "unsupported";
        public void CheckThread() { }
        private void Send(string request) => throw new NuxieException("unsupportedPlatform","Use a Unity mobile player");
        private void DetachNative() { }
#endif
    }
}
