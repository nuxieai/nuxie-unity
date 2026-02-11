using Nuxie.Unity.Internal;

namespace Nuxie.Unity.Core.Tests;

public sealed class NuxieTests : IDisposable
{
  public NuxieTests()
  {
    Nuxie.ResetForTests();
  }

  public void Dispose()
  {
    Nuxie.ResetForTests();
  }

  [Fact]
  public async Task ConfigureAsync_RequiresApiKey()
  {
    var exception = await Assert.ThrowsAsync<NuxieException>(() => Nuxie.ConfigureAsync(new NuxieConfig("")));
    Assert.Equal("MISSING_API_KEY", exception.Code);
  }

  [Fact]
  public async Task Trigger_CompletesOnlyOnTerminalUpdate()
  {
    var bridge = new FakeNativeBridge();
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"));

    var operation = sdk.Trigger("premium_tapped");

    Assert.Single(bridge.StartedTriggers);
    var requestId = operation.RequestId;

    bridge.EmitEnvelope(
      NativeEventType.TriggerUpdate,
      requestId,
      new
      {
        update = new
        {
          kind = "decision",
          decision = new
          {
            type = "flow_shown",
            @ref = new { journeyId = "j1", campaignId = "c1", flowId = "f1" },
          },
        },
      }
    );

    await Task.Delay(30);
    Assert.False(operation.Done.IsCompleted);

    bridge.EmitEnvelope(
      NativeEventType.TriggerUpdate,
      requestId,
      new
      {
        update = new
        {
          kind = "decision",
          decision = new { type = "allowed_immediate" },
        },
      }
    );

    var terminal = await operation.Done.WaitWithTimeout(TimeSpan.FromSeconds(1));
    Assert.Equal(TriggerUpdateKind.Decision, terminal.Kind);
    Assert.Equal(TriggerDecisionType.AllowedImmediate, terminal.Decision?.Type);
  }

  [Fact]
  public async Task Trigger_CancelProducesDeterministicTerminalUpdate_WhenNativeCancelThrows()
  {
    var bridge = new FakeNativeBridge { CancelShouldThrow = true };
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"));

    var operation = sdk.Trigger("premium_tapped");
    await operation.CancelAsync();
    var terminal = await operation.Done.WaitWithTimeout(TimeSpan.FromSeconds(1));

    Assert.Equal(TriggerUpdateKind.Error, terminal.Kind);
    Assert.Equal("trigger_cancelled", terminal.Error?.Code);
  }

  [Fact]
  public async Task Trigger_EmitsStartFailureAsTerminalError()
  {
    var bridge = new FakeNativeBridge { StartTriggerException = new InvalidOperationException("boom") };
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"));

    var operation = sdk.Trigger("premium_tapped");
    var terminal = await operation.Done.WaitWithTimeout(TimeSpan.FromSeconds(1));

    Assert.Equal(TriggerUpdateKind.Error, terminal.Kind);
    Assert.Equal("trigger_start_failed", terminal.Error?.Code);
  }

  [Fact]
  public async Task TriggerOnceAsync_ReturnsTimeoutTerminalAndCancelsNativeTrigger()
  {
    var bridge = new FakeNativeBridge();
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"));

    var terminal = await sdk.TriggerOnceAsync("slow_trigger", timeout: TimeSpan.FromMilliseconds(50));
    Assert.Equal(TriggerUpdateKind.Error, terminal.Kind);
    Assert.Equal("trigger_timeout", terminal.Error?.Code);
    Assert.Equal(1, bridge.CancelTriggerCalls);
  }

  [Fact]
  public async Task PurchaseRequest_CompletesUsingControllerResult()
  {
    var bridge = new FakeNativeBridge();
    var controller = new TestPurchaseController
    {
      PurchaseResultFactory = _ => Task.FromResult(PurchaseResult.Success(productId: "sku.pro")),
    };
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"), controller);

    bridge.EmitEnvelope(
      NativeEventType.PurchaseRequest,
      "purchase-1",
      new
      {
        requestId = "purchase-1",
        platform = "ios",
        productId = "sku.pro",
        timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
      }
    );

    var completion = await bridge.PurchaseCompletions.Task.WaitWithTimeout(TimeSpan.FromSeconds(1));
    Assert.Equal("purchase-1", completion.RequestId);
    Assert.Equal(PurchaseResultType.Success, completion.Result.Type);
    Assert.Equal("sku.pro", completion.Result.ProductId);
  }

  [Fact]
  public async Task PurchaseRequest_WithoutController_MapsToDeterministicFailure()
  {
    var bridge = new FakeNativeBridge();
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"));

    bridge.EmitEnvelope(
      NativeEventType.PurchaseRequest,
      "purchase-2",
      new
      {
        requestId = "purchase-2",
        platform = "android",
        productId = "sku.basic",
      }
    );

    var completion = await bridge.PurchaseCompletions.Task.WaitWithTimeout(TimeSpan.FromSeconds(1));
    Assert.Equal(PurchaseResultType.Failed, completion.Result.Type);
    Assert.Equal("purchase_delegate_not_configured", completion.Result.Message);
  }

  [Fact]
  public async Task RestoreRequest_TimeoutReturnsRestoreTimeoutFailure()
  {
    var bridge = new FakeNativeBridge();
    var controller = new TestPurchaseController
    {
      RestoreResultFactory = _ => new TaskCompletionSource<RestoreResult>(TaskCreationOptions.RunContinuationsAsynchronously).Task,
    };

    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(
      new NuxieConfig("NX_TEST")
      {
        RestoreRequestTimeoutSeconds = 1,
      },
      controller
    );

    bridge.EmitEnvelope(
      NativeEventType.RestoreRequest,
      "restore-1",
      new
      {
        requestId = "restore-1",
        platform = "ios",
      }
    );

    var completion = await bridge.RestoreCompletions.Task.WaitWithTimeout(TimeSpan.FromSeconds(2));
    Assert.Equal("restore-1", completion.RequestId);
    Assert.Equal(RestoreResultType.Failed, completion.Result.Type);
    Assert.Equal("restore_timeout", completion.Result.Message);
  }

  [Fact]
  public async Task Shutdown_CancelsInFlightTriggerOperations()
  {
    var bridge = new FakeNativeBridge();
    Nuxie.SetBridgeFactoryForTests(() => bridge);
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig("NX_TEST"));

    var operation = sdk.Trigger("event_before_shutdown");
    await sdk.ShutdownAsync();
    var terminal = await operation.Done.WaitWithTimeout(TimeSpan.FromSeconds(1));

    Assert.Equal(TriggerUpdateKind.Error, terminal.Kind);
    Assert.Equal("trigger_cancelled", terminal.Error?.Code);
  }

  private sealed class TestPurchaseController : INuxiePurchaseController
  {
    public Func<PurchaseRequest, Task<PurchaseResult>>? PurchaseResultFactory { get; init; }
    public Func<RestoreRequest, Task<RestoreResult>>? RestoreResultFactory { get; init; }

    public Task<PurchaseResult> OnPurchaseAsync(PurchaseRequest request)
    {
      return PurchaseResultFactory?.Invoke(request) ?? Task.FromResult(PurchaseResult.Success());
    }

    public Task<RestoreResult> OnRestoreAsync(RestoreRequest request)
    {
      return RestoreResultFactory?.Invoke(request) ?? Task.FromResult(RestoreResult.Success(1));
    }
  }
}
