#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Events;
namespace Nuxie.Unity
{
    public sealed class NuxieBootstrap : MonoBehaviour
    {
        [SerializeField] private NuxieSettings settings;
        public UnityEvent Configured = new UnityEvent();
        public UnityEvent<string> Failed = new UnityEvent<string>();
        private async void Start()
        {
            try { if (settings == null) throw new InvalidOperationException("Assign a Nuxie Settings asset"); await Nuxie.Client.ConfigureAsync(settings.ToOptions()); if (this != null) Configured.Invoke(); }
            catch (Exception e) { if (this != null) Failed.Invoke(e.Message); Debug.LogException(e); }
        }
    }
}
#endif
