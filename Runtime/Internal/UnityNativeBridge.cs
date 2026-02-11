using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nuxie.Unity.Internal;

internal sealed class UnityNativeBridge : INuxieNativeBridge
{
  private const string CallbackObjectName = "__NuxieBridgeHost";
  private const string CallbackMethodName = "OnNuxieNativeEvent";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
  };

  private static event Action<string>? RawNativeEventReceived;

  public event Action<NativeEventEnvelope>? EventReceived;

  public UnityNativeBridge()
  {
    RawNativeEventReceived += OnRawNativeEventReceived;
  }

  internal static void DispatchRawNativeEvent(string json)
  {
    RawNativeEventReceived?.Invoke(json);
  }

  public async Task ConfigureAsync(
    string apiKey,
    Dictionary<string, object?> options,
    bool usingPurchaseController,
    string wrapperVersion,
    CancellationToken cancellationToken
  )
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["apiKey"] = apiKey,
      ["options"] = options,
      ["usingPurchaseController"] = usingPurchaseController,
      ["wrapperVersion"] = wrapperVersion,
    };

    await InvokeVoidAsync("configure", args, cancellationToken);
  }

  public Task ShutdownAsync(CancellationToken cancellationToken)
  {
    return InvokeVoidAsync("shutdown", null, cancellationToken);
  }

  public Task IdentifyAsync(
    string distinctId,
    IReadOnlyDictionary<string, object?>? userProperties,
    IReadOnlyDictionary<string, object?>? userPropertiesSetOnce,
    CancellationToken cancellationToken
  )
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["distinctId"] = distinctId,
      ["userProperties"] = userProperties,
      ["userPropertiesSetOnce"] = userPropertiesSetOnce,
    };

    return InvokeVoidAsync("identify", args, cancellationToken);
  }

  public Task ResetAsync(bool keepAnonymousId, CancellationToken cancellationToken)
  {
    return InvokeVoidAsync("reset", new Dictionary<string, object?> { ["keepAnonymousId"] = keepAnonymousId }, cancellationToken);
  }

  public Task<string> GetDistinctIdAsync(CancellationToken cancellationToken)
  {
    return InvokeAsync("getDistinctId", null, element => element.GetString() ?? "", cancellationToken);
  }

  public Task<string> GetAnonymousIdAsync(CancellationToken cancellationToken)
  {
    return InvokeAsync("getAnonymousId", null, element => element.GetString() ?? "", cancellationToken);
  }

  public Task<bool> GetIsIdentifiedAsync(CancellationToken cancellationToken)
  {
    return InvokeAsync("getIsIdentified", null, element => element.ValueKind == JsonValueKind.True, cancellationToken);
  }

  public Task StartTriggerAsync(string requestId, string eventName, TriggerOptions? options, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["requestId"] = requestId,
      ["eventName"] = eventName,
      ["options"] = options?.ToBridgePayload(),
    };

    return InvokeVoidAsync("startTrigger", args, cancellationToken);
  }

  public Task CancelTriggerAsync(string requestId, CancellationToken cancellationToken)
  {
    return InvokeVoidAsync("cancelTrigger", new Dictionary<string, object?> { ["requestId"] = requestId }, cancellationToken);
  }

  public Task ShowFlowAsync(string flowId, CancellationToken cancellationToken)
  {
    return InvokeVoidAsync("showFlow", new Dictionary<string, object?> { ["flowId"] = flowId }, cancellationToken);
  }

  public Task<ProfileResponse> RefreshProfileAsync(CancellationToken cancellationToken)
  {
    return InvokeAsync("refreshProfile", null, NativePayloadMapper.ParseProfileResponse, cancellationToken);
  }

  public Task<FeatureAccess> HasFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["featureId"] = featureId,
      ["requiredBalance"] = requiredBalance,
      ["entityId"] = entityId,
    };

    return InvokeAsync("hasFeature", args, NativePayloadMapper.ParseFeatureAccess, cancellationToken);
  }

  public Task<FeatureAccess?> GetCachedFeatureAsync(string featureId, string? entityId, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["featureId"] = featureId,
      ["entityId"] = entityId,
    };

    return InvokeAsync(
      "getCachedFeature",
      args,
      element => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : NativePayloadMapper.ParseFeatureAccess(element),
      cancellationToken
    );
  }

  public Task<FeatureCheckResult> CheckFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["featureId"] = featureId,
      ["requiredBalance"] = requiredBalance,
      ["entityId"] = entityId,
    };

    return InvokeAsync("checkFeature", args, NativePayloadMapper.ParseFeatureCheckResult, cancellationToken);
  }

  public Task<FeatureCheckResult> RefreshFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["featureId"] = featureId,
      ["requiredBalance"] = requiredBalance,
      ["entityId"] = entityId,
    };

    return InvokeAsync("refreshFeature", args, NativePayloadMapper.ParseFeatureCheckResult, cancellationToken);
  }

  public Task UseFeatureAsync(
    string featureId,
    double amount,
    string? entityId,
    IReadOnlyDictionary<string, object?>? metadata,
    CancellationToken cancellationToken
  )
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["featureId"] = featureId,
      ["amount"] = amount,
      ["entityId"] = entityId,
      ["metadata"] = metadata,
    };

    return InvokeVoidAsync("useFeature", args, cancellationToken);
  }

  public Task<FeatureUsageResult> UseFeatureAndWaitAsync(
    string featureId,
    double amount,
    string? entityId,
    bool setUsage,
    IReadOnlyDictionary<string, object?>? metadata,
    CancellationToken cancellationToken
  )
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["featureId"] = featureId,
      ["amount"] = amount,
      ["entityId"] = entityId,
      ["setUsage"] = setUsage,
      ["metadata"] = metadata,
    };

    return InvokeAsync("useFeatureAndWait", args, NativePayloadMapper.ParseFeatureUsageResult, cancellationToken);
  }

  public Task<bool> FlushEventsAsync(CancellationToken cancellationToken)
  {
    return InvokeAsync("flushEvents", null, element => element.ValueKind == JsonValueKind.True, cancellationToken);
  }

  public Task<int> GetQueuedEventCountAsync(CancellationToken cancellationToken)
  {
    return InvokeAsync("getQueuedEventCount", null, element => element.TryGetInt32(out var value) ? value : 0, cancellationToken);
  }

  public Task PauseEventQueueAsync(CancellationToken cancellationToken)
  {
    return InvokeVoidAsync("pauseEventQueue", null, cancellationToken);
  }

  public Task ResumeEventQueueAsync(CancellationToken cancellationToken)
  {
    return InvokeVoidAsync("resumeEventQueue", null, cancellationToken);
  }

  public Task CompletePurchaseAsync(string requestId, PurchaseResult result, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["requestId"] = requestId,
      ["result"] = NativePayloadMapper.PurchaseResultToDictionary(result),
    };

    return InvokeVoidAsync("completePurchase", args, cancellationToken);
  }

  public Task CompleteRestoreAsync(string requestId, RestoreResult result, CancellationToken cancellationToken)
  {
    var args = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["requestId"] = requestId,
      ["result"] = NativePayloadMapper.RestoreResultToDictionary(result),
    };

    return InvokeVoidAsync("completeRestore", args, cancellationToken);
  }

  private void OnRawNativeEventReceived(string json)
  {
    if (!NativeEventEnvelope.TryParse(json, out var envelope, out _) || envelope is null)
    {
      return;
    }

    EventReceived?.Invoke(envelope);
  }

  private async Task InvokeVoidAsync(string method, Dictionary<string, object?>? args, CancellationToken cancellationToken)
  {
    await InvokeAsync(
      method,
      args,
      _ => true,
      cancellationToken
    );
  }

  private async Task<T> InvokeAsync<T>(
    string method,
    Dictionary<string, object?>? args,
    Func<JsonElement, T> mapper,
    CancellationToken cancellationToken
  )
  {
    cancellationToken.ThrowIfCancellationRequested();
    string raw = await Task.Run(() => InvokeNative(method, args), cancellationToken);
    if (string.IsNullOrWhiteSpace(raw))
    {
      throw new NuxieException("NATIVE_ERROR", $"Native bridge returned an empty response for '{method}'.");
    }

    using var document = JsonDocument.Parse(raw);
    var root = document.RootElement;
    var ok = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind == JsonValueKind.True;
    if (!ok)
    {
      var code = "NATIVE_ERROR";
      var message = $"Native bridge call '{method}' failed.";
      var nativeStack = default(string);
      if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
      {
        if (errorElement.TryGetProperty("code", out var codeElement))
        {
          code = codeElement.GetString() ?? code;
        }

        if (errorElement.TryGetProperty("message", out var messageElement))
        {
          message = messageElement.GetString() ?? message;
        }

        if (errorElement.TryGetProperty("nativeStack", out var stackElement))
        {
          nativeStack = stackElement.GetString();
        }
      }

      throw new NuxieException(code, message, nativeStack);
    }

    if (!root.TryGetProperty("value", out var valueElement))
    {
      valueElement = default;
    }

    return mapper(valueElement);
  }

  private string InvokeNative(string method, Dictionary<string, object?>? args)
  {
    var argsJson = JsonSerializer.Serialize(args ?? new Dictionary<string, object?>(), JsonOptions);

#if UNITY_5_3_OR_NEWER
    NuxieBridgeHost.EnsureCreated();
#endif

#if UNITY_IOS && !UNITY_EDITOR
    var pointer = NuxieUnity_Invoke(
      method,
      argsJson,
      CallbackObjectName,
      CallbackMethodName
    );

    if (pointer == IntPtr.Zero)
    {
      return "{\"ok\":false,\"error\":{\"code\":\"NATIVE_ERROR\",\"message\":\"Native invoke returned null.\"}}";
    }

    try
    {
      return Marshal.PtrToStringAnsi(pointer) ?? "";
    }
    finally
    {
      NuxieUnity_FreeCString(pointer);
    }
#elif UNITY_ANDROID && !UNITY_EDITOR
    try
    {
      using var bridgeClass = new UnityEngine.AndroidJavaClass("io.nuxie.unity.NuxieUnityBridge");
      var raw = bridgeClass.CallStatic<string>("invoke", method, argsJson, CallbackObjectName, CallbackMethodName);
      return raw ?? "{\"ok\":false,\"error\":{\"code\":\"NATIVE_ERROR\",\"message\":\"Android bridge returned null.\"}}";
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(
        new
        {
          ok = false,
          error = new
          {
            code = "NATIVE_ERROR",
            message = ex.Message,
            nativeStack = ex.ToString(),
          },
        },
        JsonOptions
      );
    }
#else
    return JsonSerializer.Serialize(
      new
      {
        ok = false,
        error = new
        {
          code = "NATIVE_ERROR",
          message = "Unity native bridge is only available on iOS/Android player builds.",
        },
      },
      JsonOptions
    );
#endif
  }

#if UNITY_IOS && !UNITY_EDITOR
  [DllImport("__Internal")]
  private static extern IntPtr NuxieUnity_Invoke(
    string method,
    string argsJson,
    string callbackObjectName,
    string callbackMethodName
  );

  [DllImport("__Internal")]
  private static extern void NuxieUnity_FreeCString(IntPtr pointer);
#endif
}
