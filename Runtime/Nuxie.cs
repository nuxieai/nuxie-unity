using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nuxie.Unity.Internal;

namespace Nuxie.Unity;

public sealed class Nuxie
{
  private const string WrapperVersionValue = "0.1.0";
  private static readonly object InstanceGate = new();
  private static long _requestCounter;
  private static Nuxie? _instance;
  private static Func<INuxieNativeBridge> _bridgeFactory = static () => new UnityNativeBridge();

  private readonly INuxieNativeBridge _bridge;
  private readonly ConcurrentDictionary<string, TriggerOperationState> _triggerOperations = new(StringComparer.Ordinal);
  private readonly SemaphoreSlim _purchaseControllerLock = new(1, 1);

  private bool _isConfigured;
  private INuxiePurchaseController? _purchaseController;
  private int _purchaseTimeoutSeconds = 60;
  private int _restoreTimeoutSeconds = 60;

  private Nuxie(INuxieNativeBridge bridge)
  {
    _bridge = bridge;
    _bridge.EventReceived += OnNativeEventReceived;
  }

  public static Nuxie Instance
  {
    get
    {
      var instance = _instance;
      if (instance is null || !instance._isConfigured)
      {
        throw new NuxieException("NOT_CONFIGURED", "Nuxie.ConfigureAsync must be called before Nuxie.Instance.");
      }

      return instance;
    }
  }

  public bool IsConfigured => _isConfigured;

  public string WrapperVersion => WrapperVersionValue;

  public event Action<TriggerUpdateEvent>? OnTriggerUpdate;
  public event Action<FeatureAccessChangedEvent>? OnFeatureAccessChanged;
  public event Action<PurchaseRequest>? OnPurchaseRequest;
  public event Action<RestoreRequest>? OnRestoreRequest;
  public event Action<FlowLifecycleEvent>? OnFlowLifecycle;

  public static async Task<Nuxie> ConfigureAsync(NuxieConfig config, INuxiePurchaseController? purchaseController = null)
  {
    if (config is null)
    {
      throw new ArgumentNullException(nameof(config));
    }

    if (string.IsNullOrWhiteSpace(config.ApiKey))
    {
      throw new NuxieException("MISSING_API_KEY", "Nuxie API key is required.");
    }

    Nuxie instance;
    lock (InstanceGate)
    {
      instance = _instance ??= new Nuxie(_bridgeFactory());
    }

    if (instance._isConfigured)
    {
      instance._purchaseController = purchaseController;
      return instance;
    }

    instance._purchaseController = purchaseController;
    instance._purchaseTimeoutSeconds = config.PurchaseRequestTimeoutSeconds;
    instance._restoreTimeoutSeconds = config.RestoreRequestTimeoutSeconds;

    try
    {
      await instance._bridge.ConfigureAsync(
        config.ApiKey,
        config.ToBridgeOptions(),
        purchaseController is not null,
        WrapperVersionValue,
        CancellationToken.None
      );
      instance._isConfigured = true;
      return instance;
    }
    catch (NuxieException)
    {
      throw;
    }
    catch (Exception ex)
    {
      throw new NuxieException("INVALID_CONFIGURATION", ex.Message, inner: ex);
    }
  }

  public async Task ShutdownAsync()
  {
    EnsureConfigured();

    foreach (var (requestId, state) in _triggerOperations)
    {
      var cancelled = TriggerUpdate.ErrorUpdate(
        new TriggerError { Code = "trigger_cancelled", Message = "Trigger cancelled during shutdown." });
      state.Emit(cancelled);
      state.TryComplete(cancelled);
      _triggerOperations.TryRemove(requestId, out _);
    }

    await _bridge.ShutdownAsync(CancellationToken.None);
    _isConfigured = false;
    _purchaseController = null;
    lock (InstanceGate)
    {
      _instance = null;
    }
  }

  public Task IdentifyAsync(
    string distinctId,
    IReadOnlyDictionary<string, object?>? userProperties = null,
    IReadOnlyDictionary<string, object?>? userPropertiesSetOnce = null
  )
  {
    EnsureConfigured();
    return _bridge.IdentifyAsync(distinctId, userProperties, userPropertiesSetOnce, CancellationToken.None);
  }

  public Task ResetAsync(bool keepAnonymousId = true)
  {
    EnsureConfigured();
    return _bridge.ResetAsync(keepAnonymousId, CancellationToken.None);
  }

  public Task<string> GetDistinctIdAsync()
  {
    EnsureConfigured();
    return _bridge.GetDistinctIdAsync(CancellationToken.None);
  }

  public Task<string> GetAnonymousIdAsync()
  {
    EnsureConfigured();
    return _bridge.GetAnonymousIdAsync(CancellationToken.None);
  }

  public Task<bool> GetIsIdentifiedAsync()
  {
    EnsureConfigured();
    return _bridge.GetIsIdentifiedAsync(CancellationToken.None);
  }

  public NuxieTriggerOperation Trigger(string eventName, TriggerOptions? options = null)
  {
    EnsureConfigured();
    var requestId = $"trigger-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Interlocked.Increment(ref _requestCounter)}";
    var state = new TriggerOperationState();
    _triggerOperations[requestId] = state;

    _ = StartTriggerAsync(requestId, eventName, options, state);
    return new NuxieTriggerOperation(requestId, state, () => CancelTriggerAsync(requestId));
  }

  public async Task<TriggerTerminalUpdate> TriggerOnceAsync(
    string eventName,
    TriggerOptions? options = null,
    TimeSpan? timeout = null
  )
  {
    var operation = Trigger(eventName, options);
    if (timeout is null)
    {
      return await operation.Done;
    }

    using var timeoutCts = new CancellationTokenSource(timeout.Value);
    var completed = await Task.WhenAny(operation.Done, WaitForTimeoutAsync(timeoutCts.Token));
    if (completed == operation.Done)
    {
      return await operation.Done;
    }

    await operation.CancelAsync();
    return TriggerTerminalUpdate.From(TriggerUpdate.ErrorUpdate(new TriggerError
    {
      Code = "trigger_timeout",
      Message = "Trigger operation timed out.",
    }));
  }

  public Task ShowFlowAsync(string flowId)
  {
    EnsureConfigured();
    return _bridge.ShowFlowAsync(flowId, CancellationToken.None);
  }

  public Task<ProfileResponse> RefreshProfileAsync()
  {
    EnsureConfigured();
    return _bridge.RefreshProfileAsync(CancellationToken.None);
  }

  public Task<FeatureAccess> HasFeatureAsync(string featureId, int? requiredBalance = null, string? entityId = null)
  {
    EnsureConfigured();
    return _bridge.HasFeatureAsync(featureId, requiredBalance, entityId, CancellationToken.None);
  }

  public Task<FeatureAccess?> GetCachedFeatureAsync(string featureId, string? entityId = null)
  {
    EnsureConfigured();
    return _bridge.GetCachedFeatureAsync(featureId, entityId, CancellationToken.None);
  }

  public Task<FeatureCheckResult> CheckFeatureAsync(string featureId, int? requiredBalance = null, string? entityId = null)
  {
    EnsureConfigured();
    return _bridge.CheckFeatureAsync(featureId, requiredBalance, entityId, CancellationToken.None);
  }

  public Task<FeatureCheckResult> RefreshFeatureAsync(string featureId, int? requiredBalance = null, string? entityId = null)
  {
    EnsureConfigured();
    return _bridge.RefreshFeatureAsync(featureId, requiredBalance, entityId, CancellationToken.None);
  }

  public Task UseFeatureAsync(
    string featureId,
    double amount = 1,
    string? entityId = null,
    IReadOnlyDictionary<string, object?>? metadata = null
  )
  {
    EnsureConfigured();
    return _bridge.UseFeatureAsync(featureId, amount, entityId, metadata, CancellationToken.None);
  }

  public Task<FeatureUsageResult> UseFeatureAndWaitAsync(
    string featureId,
    double amount = 1,
    string? entityId = null,
    bool setUsage = false,
    IReadOnlyDictionary<string, object?>? metadata = null
  )
  {
    EnsureConfigured();
    return _bridge.UseFeatureAndWaitAsync(
      featureId,
      amount,
      entityId,
      setUsage,
      metadata,
      CancellationToken.None
    );
  }

  public Task<bool> FlushEventsAsync()
  {
    EnsureConfigured();
    return _bridge.FlushEventsAsync(CancellationToken.None);
  }

  public Task<int> GetQueuedEventCountAsync()
  {
    EnsureConfigured();
    return _bridge.GetQueuedEventCountAsync(CancellationToken.None);
  }

  public Task PauseEventQueueAsync()
  {
    EnsureConfigured();
    return _bridge.PauseEventQueueAsync(CancellationToken.None);
  }

  public Task ResumeEventQueueAsync()
  {
    EnsureConfigured();
    return _bridge.ResumeEventQueueAsync(CancellationToken.None);
  }

  internal static void SetBridgeFactoryForTests(Func<INuxieNativeBridge> bridgeFactory)
  {
    _bridgeFactory = bridgeFactory ?? throw new ArgumentNullException(nameof(bridgeFactory));
  }

  internal static void ResetForTests()
  {
    lock (InstanceGate)
    {
      _instance = null;
      _bridgeFactory = static () => new UnityNativeBridge();
      Interlocked.Exchange(ref _requestCounter, 0);
    }
  }

  private async Task StartTriggerAsync(string requestId, string eventName, TriggerOptions? options, TriggerOperationState state)
  {
    try
    {
      await _bridge.StartTriggerAsync(requestId, eventName, options, CancellationToken.None);
    }
    catch (Exception ex)
    {
      var update = TriggerUpdate.ErrorUpdate(new TriggerError
      {
        Code = "trigger_start_failed",
        Message = ex.Message,
      });
      state.Emit(update);
      state.TryComplete(update);
      _triggerOperations.TryRemove(requestId, out _);
    }
  }

  private async Task CancelTriggerAsync(string requestId)
  {
    if (!_triggerOperations.TryRemove(requestId, out var state))
    {
      return;
    }

    try
    {
      await _bridge.CancelTriggerAsync(requestId, CancellationToken.None);
    }
    catch
    {
      // Ignore native cancel failures to keep cancellation deterministic.
    }

    var cancelled = TriggerUpdate.ErrorUpdate(new TriggerError
    {
      Code = "trigger_cancelled",
      Message = "Trigger operation was cancelled.",
    });
    state.Emit(cancelled);
    state.TryComplete(cancelled);
  }

  private async void OnNativeEventReceived(NativeEventEnvelope envelope)
  {
    switch (envelope.Type)
    {
      case NativeEventType.TriggerUpdate:
        HandleTriggerUpdate(envelope);
        break;
      case NativeEventType.FeatureAccessChanged:
        OnFeatureAccessChanged?.Invoke(NativePayloadMapper.ParseFeatureAccessChanged(envelope));
        break;
      case NativeEventType.PurchaseRequest:
        await HandlePurchaseRequestAsync(envelope);
        break;
      case NativeEventType.RestoreRequest:
        await HandleRestoreRequestAsync(envelope);
        break;
      case NativeEventType.FlowPresented:
      case NativeEventType.FlowDismissed:
        OnFlowLifecycle?.Invoke(NativePayloadMapper.ParseFlowLifecycleEvent(envelope));
        break;
      case NativeEventType.Unknown:
      default:
        break;
    }
  }

  private void HandleTriggerUpdate(NativeEventEnvelope envelope)
  {
    var requestId = envelope.RequestId ?? "";
    var update = NativePayloadMapper.ParseTriggerUpdate(envelope.Payload);
    var terminalFromNative = NativePayloadMapper.TryGetNativeTerminalFlag(envelope.Payload, out var nativeTerminal) && nativeTerminal;
    var terminal = terminalFromNative || update.IsTerminal;

    var updateEvent = new TriggerUpdateEvent
    {
      RequestId = requestId,
      Update = update,
      IsTerminal = terminal,
      TimestampMs = envelope.TimestampMs,
    };
    OnTriggerUpdate?.Invoke(updateEvent);

    if (!_triggerOperations.TryGetValue(requestId, out var state))
    {
      return;
    }

    state.Emit(update);
    if (terminal)
    {
      state.TryComplete(update);
      _triggerOperations.TryRemove(requestId, out _);
    }
  }

  private async Task HandlePurchaseRequestAsync(NativeEventEnvelope envelope)
  {
    var request = NativePayloadMapper.ParsePurchaseRequest(envelope);
    OnPurchaseRequest?.Invoke(request);

    await _purchaseControllerLock.WaitAsync();
    try
    {
      PurchaseResult result;
      if (_purchaseController is null)
      {
        result = PurchaseResult.Failed("purchase_delegate_not_configured");
      }
      else
      {
        result = await ResolvePurchaseResultWithTimeoutAsync(request);
      }

      await _bridge.CompletePurchaseAsync(request.RequestId, result, CancellationToken.None);
    }
    finally
    {
      _purchaseControllerLock.Release();
    }
  }

  private async Task HandleRestoreRequestAsync(NativeEventEnvelope envelope)
  {
    var request = NativePayloadMapper.ParseRestoreRequest(envelope);
    OnRestoreRequest?.Invoke(request);

    await _purchaseControllerLock.WaitAsync();
    try
    {
      RestoreResult result;
      if (_purchaseController is null)
      {
        result = RestoreResult.Failed("purchase_delegate_not_configured");
      }
      else
      {
        result = await ResolveRestoreResultWithTimeoutAsync(request);
      }

      await _bridge.CompleteRestoreAsync(request.RequestId, result, CancellationToken.None);
    }
    finally
    {
      _purchaseControllerLock.Release();
    }
  }

  private async Task<PurchaseResult> ResolvePurchaseResultWithTimeoutAsync(PurchaseRequest request)
  {
    try
    {
      if (_purchaseController is null)
      {
        return PurchaseResult.Failed("purchase_delegate_not_configured");
      }

      using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _purchaseTimeoutSeconds)));
      var opTask = _purchaseController.OnPurchaseAsync(request);
      var winner = await Task.WhenAny(opTask, WaitForTimeoutAsync(timeoutCts.Token));
      if (winner == opTask)
      {
        return await opTask;
      }

      return PurchaseResult.Failed("purchase_timeout");
    }
    catch (Exception ex)
    {
      return PurchaseResult.Failed(ex.Message);
    }
  }

  private async Task<RestoreResult> ResolveRestoreResultWithTimeoutAsync(RestoreRequest request)
  {
    try
    {
      if (_purchaseController is null)
      {
        return RestoreResult.Failed("purchase_delegate_not_configured");
      }

      using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _restoreTimeoutSeconds)));
      var opTask = _purchaseController.OnRestoreAsync(request);
      var winner = await Task.WhenAny(opTask, WaitForTimeoutAsync(timeoutCts.Token));
      if (winner == opTask)
      {
        return await opTask;
      }

      return RestoreResult.Failed("restore_timeout");
    }
    catch (Exception ex)
    {
      return RestoreResult.Failed(ex.Message);
    }
  }

  private static async Task WaitForTimeoutAsync(CancellationToken cancellationToken)
  {
    try
    {
      await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      // Expected
    }
  }

  private void EnsureConfigured()
  {
    if (!_isConfigured)
    {
      throw new NuxieException("NOT_CONFIGURED", "Nuxie SDK has not been configured.");
    }
  }
}
