#if UNITY_5_3_OR_NEWER
using Nuxie.Unity.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Nuxie.Unity;

[Preserve]
public sealed class NuxieBridgeHost : MonoBehaviour
{
  private const string HostName = "__NuxieBridgeHost";
  private const string CallbackMethod = "OnNuxieNativeEvent";
  private static NuxieBridgeHost? _instance;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  private static void EnsureCreatedOnLoad()
  {
    EnsureCreated();
  }

  public static string CallbackObjectName => HostName;
  public static string CallbackMethodName => CallbackMethod;

  public static NuxieBridgeHost EnsureCreated()
  {
    if (_instance is not null)
    {
      return _instance;
    }

    var existing = GameObject.Find(HostName);
    if (existing is not null)
    {
      _instance = existing.GetComponent<NuxieBridgeHost>();
      if (_instance is not null)
      {
        return _instance;
      }
    }

    var gameObject = new GameObject(HostName);
    DontDestroyOnLoad(gameObject);
    _instance = gameObject.AddComponent<NuxieBridgeHost>();
    return _instance;
  }

  [Preserve]
  public void OnNuxieNativeEvent(string envelopeJson)
  {
    UnityNativeBridge.DispatchRawNativeEvent(envelopeJson);
  }
}
#endif
