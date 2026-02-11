using System.Collections.Concurrent;
using System.Text.Json;
using Nuxie.Unity.Internal;

namespace Nuxie.Unity.Core.Tests;

internal sealed class FakeNativeBridge : INuxieNativeBridge
{
  private readonly ConcurrentDictionary<string, TriggerOptions?> _startedTriggers = new();

  public event Action<NativeEventEnvelope>? EventReceived;

  public int ConfigureCalls { get; private set; }
  public int CancelTriggerCalls { get; private set; }
  public Exception? StartTriggerException { get; set; }
  public bool CancelShouldThrow { get; set; }

  public TaskCompletionSource<(string RequestId, PurchaseResult Result)> PurchaseCompletions { get; } =
    new(TaskCreationOptions.RunContinuationsAsynchronously);

  public TaskCompletionSource<(string RequestId, RestoreResult Result)> RestoreCompletions { get; } =
    new(TaskCreationOptions.RunContinuationsAsynchronously);

  public IReadOnlyDictionary<string, TriggerOptions?> StartedTriggers => _startedTriggers;

  public Task ConfigureAsync(
    string apiKey,
    Dictionary<string, object?> options,
    bool usingPurchaseController,
    string wrapperVersion,
    CancellationToken cancellationToken
  )
  {
    ConfigureCalls += 1;
    return Task.CompletedTask;
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
    return Task.CompletedTask;
  }

  public Task ResetAsync(bool keepAnonymousId, CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task<string> GetDistinctIdAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult("distinct-1");
  }

  public Task<string> GetAnonymousIdAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult("anon-1");
  }

  public Task<bool> GetIsIdentifiedAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(true);
  }

  public Task StartTriggerAsync(
    string requestId,
    string eventName,
    TriggerOptions? options,
    CancellationToken cancellationToken
  )
  {
    if (StartTriggerException is not null)
    {
      throw StartTriggerException;
    }

    _startedTriggers[requestId] = options;
    return Task.CompletedTask;
  }

  public Task CancelTriggerAsync(string requestId, CancellationToken cancellationToken)
  {
    CancelTriggerCalls += 1;
    if (CancelShouldThrow)
    {
      throw new InvalidOperationException("cancel failed");
    }

    return Task.CompletedTask;
  }

  public Task ShowFlowAsync(string flowId, CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task<ProfileResponse> RefreshProfileAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(new ProfileResponse { CustomerId = "customer-1" });
  }

  public Task<FeatureAccess> HasFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    return Task.FromResult(new FeatureAccess { Allowed = true, Unlimited = true, Type = FeatureType.Boolean });
  }

  public Task<FeatureAccess?> GetCachedFeatureAsync(string featureId, string? entityId, CancellationToken cancellationToken)
  {
    return Task.FromResult<FeatureAccess?>(new FeatureAccess { Allowed = true, Unlimited = false, Type = FeatureType.Metered, Balance = 5 });
  }

  public Task<FeatureCheckResult> CheckFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    return Task.FromResult(new FeatureCheckResult
    {
      CustomerId = "customer-1",
      FeatureId = featureId,
      RequiredBalance = requiredBalance ?? 1,
      Code = "allowed",
      Allowed = true,
      Unlimited = false,
      Balance = 10,
      Type = FeatureType.Metered,
    });
  }

  public Task<FeatureCheckResult> RefreshFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken)
  {
    return CheckFeatureAsync(featureId, requiredBalance, entityId, cancellationToken);
  }

  public Task UseFeatureAsync(
    string featureId,
    double amount,
    string? entityId,
    IReadOnlyDictionary<string, object?>? metadata,
    CancellationToken cancellationToken
  )
  {
    return Task.CompletedTask;
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
    return Task.FromResult(new FeatureUsageResult
    {
      Success = true,
      FeatureId = featureId,
      AmountUsed = amount,
      Usage = new FeatureUsageInfo { Current = amount, Remaining = 9 },
    });
  }

  public Task<bool> FlushEventsAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(true);
  }

  public Task<int> GetQueuedEventCountAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(0);
  }

  public Task PauseEventQueueAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task ResumeEventQueueAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task CompletePurchaseAsync(string requestId, PurchaseResult result, CancellationToken cancellationToken)
  {
    PurchaseCompletions.TrySetResult((requestId, result));
    return Task.CompletedTask;
  }

  public Task CompleteRestoreAsync(string requestId, RestoreResult result, CancellationToken cancellationToken)
  {
    RestoreCompletions.TrySetResult((requestId, result));
    return Task.CompletedTask;
  }

  public void EmitEnvelope(string jsonEnvelope)
  {
    if (!NativeEventEnvelope.TryParse(jsonEnvelope, out var envelope, out var error))
    {
      throw new InvalidOperationException(error ?? "failed to parse envelope");
    }

    EventReceived?.Invoke(envelope!);
  }

  public void EmitEnvelope(NativeEventType type, string? requestId, object payload, long? timestampMs = null)
  {
    var typeString = type switch
    {
      NativeEventType.TriggerUpdate => "trigger_update",
      NativeEventType.FeatureAccessChanged => "feature_access_changed",
      NativeEventType.PurchaseRequest => "purchase_request",
      NativeEventType.RestoreRequest => "restore_request",
      NativeEventType.FlowPresented => "flow_presented",
      NativeEventType.FlowDismissed => "flow_dismissed",
      _ => "unknown",
    };

    var json = JsonSerializer.Serialize(new
    {
      type = typeString,
      requestId,
      timestampMs = timestampMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
      payload,
    });

    EmitEnvelope(json);
  }
}
