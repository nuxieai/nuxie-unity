using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nuxie.Unity.Internal;

internal interface INuxieNativeBridge
{
  event Action<NativeEventEnvelope>? EventReceived;

  Task ConfigureAsync(
    string apiKey,
    Dictionary<string, object?> options,
    bool usingPurchaseController,
    string wrapperVersion,
    CancellationToken cancellationToken
  );

  Task ShutdownAsync(CancellationToken cancellationToken);

  Task IdentifyAsync(
    string distinctId,
    IReadOnlyDictionary<string, object?>? userProperties,
    IReadOnlyDictionary<string, object?>? userPropertiesSetOnce,
    CancellationToken cancellationToken
  );

  Task ResetAsync(bool keepAnonymousId, CancellationToken cancellationToken);
  Task<string> GetDistinctIdAsync(CancellationToken cancellationToken);
  Task<string> GetAnonymousIdAsync(CancellationToken cancellationToken);
  Task<bool> GetIsIdentifiedAsync(CancellationToken cancellationToken);

  Task StartTriggerAsync(
    string requestId,
    string eventName,
    TriggerOptions? options,
    CancellationToken cancellationToken
  );

  Task CancelTriggerAsync(string requestId, CancellationToken cancellationToken);
  Task ShowFlowAsync(string flowId, CancellationToken cancellationToken);
  Task<ProfileResponse> RefreshProfileAsync(CancellationToken cancellationToken);
  Task<FeatureAccess> HasFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken);
  Task<FeatureAccess?> GetCachedFeatureAsync(string featureId, string? entityId, CancellationToken cancellationToken);
  Task<FeatureCheckResult> CheckFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken);
  Task<FeatureCheckResult> RefreshFeatureAsync(string featureId, int? requiredBalance, string? entityId, CancellationToken cancellationToken);
  Task UseFeatureAsync(
    string featureId,
    double amount,
    string? entityId,
    IReadOnlyDictionary<string, object?>? metadata,
    CancellationToken cancellationToken
  );

  Task<FeatureUsageResult> UseFeatureAndWaitAsync(
    string featureId,
    double amount,
    string? entityId,
    bool setUsage,
    IReadOnlyDictionary<string, object?>? metadata,
    CancellationToken cancellationToken
  );

  Task<bool> FlushEventsAsync(CancellationToken cancellationToken);
  Task<int> GetQueuedEventCountAsync(CancellationToken cancellationToken);
  Task PauseEventQueueAsync(CancellationToken cancellationToken);
  Task ResumeEventQueueAsync(CancellationToken cancellationToken);
  Task CompletePurchaseAsync(string requestId, PurchaseResult result, CancellationToken cancellationToken);
  Task CompleteRestoreAsync(string requestId, RestoreResult result, CancellationToken cancellationToken);
}
