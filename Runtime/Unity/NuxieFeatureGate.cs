#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Events;
namespace Nuxie.Unity
{
    public sealed class NuxieFeatureGate : MonoBehaviour
    {
        [SerializeField] private string featureId;
        public UnityEvent Unknown = new UnityEvent();
        public UnityEvent Allowed = new UnityEvent();
        public UnityEvent Denied = new UnityEvent();
        private IDisposable subscription;
        private int activationVersion;
        private void OnEnable()
        {
            var version = ++activationVersion;
            var observer = Nuxie.Client.ObserveFeature(featureId, state => {
                if (state.State == FeatureStateKind.Unknown) Unknown.Invoke();
                else if (state.Access?.Allowed == true) Allowed.Invoke();
                else Denied.Invoke();
            });
            // The immediate notification may disable, destroy or re-enable this gate.
            if (this != null && isActiveAndEnabled && version == activationVersion) subscription = observer;
            else observer.Dispose();
        }
        private void OnDisable() { activationVersion++; subscription?.Dispose(); subscription = null; }
    }
}
#endif
