using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Nuxie.Unity.Samples;

public sealed class NuxieDemoController : MonoBehaviour, INuxiePurchaseController
{
  [SerializeField] private string apiKey = "NX_REPLACE_ME";
  [SerializeField] private string distinctId = "unity-demo-user";
  [SerializeField] private string triggerEventName = "paywall_trigger";
  [SerializeField] private string flowId = "";

  private Nuxie? _sdk;
  private NuxieTriggerOperation? _triggerOperation;
  private IDisposable? _triggerSubscription;

  private async void Start()
  {
    await InitializeAsync();
  }

  private void OnDestroy()
  {
    _triggerSubscription?.Dispose();
    _triggerSubscription = null;
  }

  [ContextMenu("Initialize Nuxie")]
  public async Task InitializeAsync()
  {
    if (_sdk is not null)
    {
      return;
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
      Debug.LogError("Nuxie API key is required.");
      return;
    }

    try
    {
      _sdk = await Nuxie.ConfigureAsync(new NuxieConfig(apiKey)
      {
        Environment = NuxieEnvironment.Production,
        LogLevel = NuxieLogLevel.Info,
        FlushAt = 20,
        FlushIntervalSeconds = 30,
      }, this);

      _sdk.OnFeatureAccessChanged += OnFeatureAccessChanged;
      _sdk.OnFlowLifecycle += OnFlowLifecycle;
      _sdk.OnPurchaseRequest += OnPurchaseRequest;
      _sdk.OnRestoreRequest += OnRestoreRequest;

      await _sdk.IdentifyAsync(
        distinctId,
        userProperties: new Dictionary<string, object?>
        {
          ["platform"] = "unity",
          ["appVersion"] = Application.version,
        }
      );

      Debug.Log("Nuxie initialized.");
    }
    catch (Exception error)
    {
      Debug.LogException(error);
    }
  }

  [ContextMenu("Trigger Event")]
  public async Task TriggerEventAsync()
  {
    if (_sdk is null)
    {
      Debug.LogWarning("Nuxie is not initialized.");
      return;
    }

    _triggerSubscription?.Dispose();
    _triggerOperation = _sdk.Trigger(
      triggerEventName,
      new TriggerOptions
      {
        Properties = new Dictionary<string, object?>
        {
          ["screen"] = "sample",
          ["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        },
      }
    );

    _triggerSubscription = _triggerOperation.OnUpdate(update =>
      Debug.Log($"[Nuxie] Trigger update: {update.Kind}"));

    try
    {
      var terminal = await _triggerOperation.Done;
      Debug.Log($"[Nuxie] Trigger terminal: {terminal.Kind}");
    }
    catch (Exception error)
    {
      Debug.LogException(error);
    }
  }

  [ContextMenu("Show Flow")]
  public async Task ShowFlowAsync()
  {
    if (_sdk is null || string.IsNullOrWhiteSpace(flowId))
    {
      Debug.LogWarning("Set flowId and initialize Nuxie first.");
      return;
    }

    try
    {
      await _sdk.ShowFlowAsync(flowId);
      Debug.Log($"[Nuxie] Show flow requested: {flowId}");
    }
    catch (Exception error)
    {
      Debug.LogException(error);
    }
  }

  [ContextMenu("Refresh Profile")]
  public async Task RefreshProfileAsync()
  {
    if (_sdk is null)
    {
      return;
    }

    try
    {
      var profile = await _sdk.RefreshProfileAsync();
      Debug.Log($"[Nuxie] Profile refreshed. customerId={profile.CustomerId}");
    }
    catch (Exception error)
    {
      Debug.LogException(error);
    }
  }

  [ContextMenu("Shutdown Nuxie")]
  public async Task ShutdownAsync()
  {
    if (_sdk is null)
    {
      return;
    }

    try
    {
      await _sdk.ShutdownAsync();
      _sdk = null;
      Debug.Log("Nuxie shutdown complete.");
    }
    catch (Exception error)
    {
      Debug.LogException(error);
    }
  }

  public Task<PurchaseResult> OnPurchaseAsync(PurchaseRequest request)
  {
    Debug.Log($"[Nuxie] Purchase request: {request.ProductId} ({request.Platform})");
    return Task.FromResult(PurchaseResult.Failed("purchase_not_implemented"));
  }

  public Task<RestoreResult> OnRestoreAsync(RestoreRequest request)
  {
    Debug.Log($"[Nuxie] Restore request ({request.Platform})");
    return Task.FromResult(RestoreResult.NoPurchases());
  }

  private void OnFeatureAccessChanged(FeatureAccessChangedEvent payload)
  {
    Debug.Log($"[Nuxie] Feature changed: {payload.FeatureId} allowed={payload.To.Allowed}");
  }

  private void OnFlowLifecycle(FlowLifecycleEvent payload)
  {
    Debug.Log($"[Nuxie] Flow lifecycle: {payload.Type} flowId={payload.FlowId}");
  }

  private void OnPurchaseRequest(PurchaseRequest payload)
  {
    Debug.Log($"[Nuxie] Purchase callback for {payload.ProductId}");
  }

  private void OnRestoreRequest(RestoreRequest payload)
  {
    Debug.Log("[Nuxie] Restore callback received");
  }
}
