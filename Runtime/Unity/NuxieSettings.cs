#if UNITY_5_3_OR_NEWER
using UnityEngine;
namespace Nuxie.Unity
{
    [CreateAssetMenu(menuName = "Nuxie/Settings", fileName = "NuxieSettings")]
    public sealed class NuxieSettings : ScriptableObject
    {
        [SerializeField] private string iosApiKey;
        [SerializeField] private string androidApiKey;
        [SerializeField] private NuxieEnvironment environment = NuxieEnvironment.Development;
        [SerializeField] private NuxieLogLevel logLevel = NuxieLogLevel.Warning;
        public NuxieOptions ToOptions() => new NuxieOptions { IosApiKey = iosApiKey, AndroidApiKey = androidApiKey, Environment = environment, LogLevel = logLevel };
    }
}
#endif
