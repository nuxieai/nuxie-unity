#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Events;
namespace Nuxie.Unity
{
    public sealed class NuxieTrigger : MonoBehaviour
    {
        [SerializeField] private string eventName;
        public UnityEvent<string> Failed = new UnityEvent<string>();
        public async void Trigger()
        { try { await Nuxie.Client.TriggerAsync(eventName); } catch (Exception e) { if (this != null) Failed.Invoke(e.Message); Debug.LogException(e); } }
    }
}
#endif
