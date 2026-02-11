# API Reference

## Static Entry

`Nuxie.ConfigureAsync(NuxieConfig config, INuxiePurchaseController? purchaseController = null)`

- Creates/configures singleton.
- Throws `NuxieException` when configuration fails.

`Nuxie.Instance`

- Returns configured singleton.
- Throws `NuxieException("NOT_CONFIGURED", ...)` if called before configure.

## Lifecycle and Identity

- `Task ShutdownAsync()`
- `Task IdentifyAsync(string distinctId, IReadOnlyDictionary<string, object?>? userProperties = null, IReadOnlyDictionary<string, object?>? userPropertiesSetOnce = null)`
- `Task ResetAsync(bool keepAnonymousId = true)`
- `Task<string> GetDistinctIdAsync()`
- `Task<string> GetAnonymousIdAsync()`
- `Task<bool> GetIsIdentifiedAsync()`

## Trigger API

- `NuxieTriggerOperation Trigger(string eventName, TriggerOptions? options = null)`
- `Task<TriggerTerminalUpdate> TriggerOnceAsync(string eventName, TriggerOptions? options = null, TimeSpan? timeout = null)`

### `NuxieTriggerOperation`

- `string RequestId`
- `Task<TriggerTerminalUpdate> Done`
- `IDisposable OnUpdate(Action<TriggerUpdate> listener)`
- `Task CancelAsync()`

### Terminal Rules

Terminal updates:

- `TriggerUpdateKind.Error`
- `TriggerUpdateKind.Journey`
- Decision: `AllowedImmediate`, `DeniedImmediate`, `NoMatch`, `Suppressed`
- Entitlement: `Allowed`, `Denied`

Non-terminal updates:

- Decision: `JourneyStarted`, `JourneyResumed`, `FlowShown`
- Entitlement: `Pending`

## Flow and Profile

- `Task ShowFlowAsync(string flowId)`
- `Task<ProfileResponse> RefreshProfileAsync()`

## Feature APIs

- `Task<FeatureAccess> HasFeatureAsync(string featureId, int? requiredBalance = null, string? entityId = null)`
- `Task<FeatureAccess?> GetCachedFeatureAsync(string featureId, string? entityId = null)`
- `Task<FeatureCheckResult> CheckFeatureAsync(string featureId, int? requiredBalance = null, string? entityId = null)`
- `Task<FeatureCheckResult> RefreshFeatureAsync(string featureId, int? requiredBalance = null, string? entityId = null)`
- `Task UseFeatureAsync(string featureId, double amount = 1, string? entityId = null, IReadOnlyDictionary<string, object?>? metadata = null)`
- `Task<FeatureUsageResult> UseFeatureAndWaitAsync(string featureId, double amount = 1, string? entityId = null, bool setUsage = false, IReadOnlyDictionary<string, object?>? metadata = null)`

## Event Queue

- `Task<bool> FlushEventsAsync()`
- `Task<int> GetQueuedEventCountAsync()`
- `Task PauseEventQueueAsync()`
- `Task ResumeEventQueueAsync()`

## Events

- `event Action<TriggerUpdateEvent>? OnTriggerUpdate`
- `event Action<FeatureAccessChangedEvent>? OnFeatureAccessChanged`
- `event Action<PurchaseRequest>? OnPurchaseRequest`
- `event Action<RestoreRequest>? OnRestoreRequest`
- `event Action<FlowLifecycleEvent>? OnFlowLifecycle`

## Purchase Controller

Implement `INuxiePurchaseController`:

```csharp
public interface INuxiePurchaseController
{
  Task<PurchaseResult> OnPurchaseAsync(PurchaseRequest request);
  Task<RestoreResult> OnRestoreAsync(RestoreRequest request);
}
```

`PurchaseResult.Type`: `Success | Cancelled | Pending | Failed`

`RestoreResult.Type`: `Success | NoPurchases | Failed`

## Unity Coroutine Helpers

For coroutine-driven projects, use `Runtime/Unity/NuxieTaskExtensions.cs`:

- `IEnumerator AsCoroutine(this Task task, Action<Exception>? onError = null)`
- `IEnumerator AsCoroutine<T>(this Task<T> task, Action<T>? onSuccess = null, Action<Exception>? onError = null)`

## Error Model

`NuxieException` exposes:

- `Code`
- `NativeStack` (optional)
- `Message`

Representative codes:

- `MISSING_API_KEY`
- `NOT_CONFIGURED`
- `INVALID_CONFIGURATION`
- `NATIVE_ERROR`
- `trigger_start_failed`
- `trigger_cancelled`
- `trigger_timeout`
- `purchase_delegate_not_configured`
- `restore_timeout`
