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
        private void OnEnable()
        { subscription = Nuxie.Client.ObserveFeature(featureId, state => { if (state.State == FeatureStateKind.Unknown) Unknown.Invoke(); else if (state.Access?.Allowed == true) Allowed.Invoke(); else Denied.Invoke(); }); }
        private void OnDisable() { subscription?.Dispose(); subscription = null; }
    }
}
#endif
