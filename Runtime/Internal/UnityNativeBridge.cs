using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nuxie.Unity.Internal;

internal sealed class UnityNativeBridge : INuxieNativeBridge
{
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

  public Task ConfigureAsync(
    string apiKey,
    Dictionary<string, object?> options,
    bool usingPurchaseController,
    string wrapperVersion,
    CancellationToken cancellationToken
  )
  {
    throw new NuxieException(
      "NATIVE_ERROR",
      "Unity native bridge is not yet wired for this runtime. Use a platform build or inject a test bridge.");
  }

  public Task ShutdownAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task IdentifyAsync(
    string distinctId,
    IReadOnlyDictionary<string, object?>? userProperties,
    IReadOnlyDictionary<string, object?>? userPropertiesSetOnce,
    CancellationToken cancellationToken
  )
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task ResetAsync(bool keepAnonymousId, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<string> GetDistinctIdAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<string> GetAnonymousIdAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<bool> GetIsIdentifiedAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task StartTriggerAsync(
    string requestId,
    string eventName,
    TriggerOptions? options,
    CancellationToken cancellationToken
  )
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task CancelTriggerAsync(string requestId, CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task ShowFlowAsync(string flowId, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<ProfileResponse> RefreshProfileAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<FeatureAccess> HasFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<FeatureAccess?> GetCachedFeatureAsync(string featureId, string? entityId, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<FeatureCheckResult> CheckFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<FeatureCheckResult> RefreshFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task UseFeatureAsync(
    string featureId,
    double amount,
    string? entityId,
    IReadOnlyDictionary<string, object?>? metadata,
    CancellationToken cancellationToken
  )
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
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
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<bool> FlushEventsAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task<int> GetQueuedEventCountAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task PauseEventQueueAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task ResumeEventQueueAsync(CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task CompletePurchaseAsync(string requestId, PurchaseResult result, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  public Task CompleteRestoreAsync(string requestId, RestoreResult result, CancellationToken cancellationToken)
  {
    throw new NuxieException("NATIVE_ERROR", "Native bridge is unavailable.");
  }

  private void OnRawNativeEventReceived(string json)
  {
    if (!NativeEventEnvelope.TryParse(json, out var envelope, out _) || envelope is null)
    {
      return;
    }

    EventReceived?.Invoke(envelope);
  }
}
