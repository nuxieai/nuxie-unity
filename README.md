# Nuxie for Unity

**Native Experiences. Live Feature access. Built for your game.**

This is the breaking **0.2** SDK, replacing the old interface completely. It is not published yet. Use the source checkout or a prepared package while evaluating this revision.

Bring remotely authored onboarding, offers, paywalls and other Experiences into your iOS and Android game. Nuxie's native SDKs handle presentation, purchases and access; your C# code decides when the moment is right.

```csharp
await Nuxie.Client.TriggerAsync("shop_requested");
```

Triggering records an event. The native SDK decides whether a Journey should run. The returned Task does not mean an Experience was shown or a purchase completed.

## One package, both mobile platforms

| Environment | Integration |
| --- | --- |
| Unity 6, iOS IL2CPP | Native Nuxie presentation and commerce |
| Unity 6, Android IL2CPP | Activity and GameActivity hosts |
| Unity Editor | Explicit simulator for game integration testing |
| Desktop players, WebGL, consoles | Unsupported in this release |

The example targets **Unity 6000.3.24f1**, iOS 15+ and Android API 25+ with IL2CPP arm64. See [qualification](Documentation~/testing-and-validation.md) for completed checks and limitations, and [native pins](NATIVE-PINS.json) for the exact SDK inputs. Editor simulation is not a backend or store test.

## Install

For this source checkout:

```sh
python3 scripts/prepare-native.py
python3 scripts/pack.py
```

The pack command also stages `.native/package`, which the example references. Do not install the repository root as a local package: it contains the example project itself. Open `ExampleProject` in Unity Hub, or use **Window → Package Manager → Install package from tarball** and select `com.nuxie.unity-0.2.0.tgz`. Published releases use immutable versioned packages; the old `0.1.0` tag does not contain this interface.

Nuxie is one UPM package, `com.nuxie.unity`. It declares its managed dependencies and carries prepared Android artifacts. The iOS export hook adds the pinned native Swift package. You do not manually edit AppDelegate or replace your Android Activity.

Import **SDK Lab** from the package's Samples tab to explore the interface. The repository also includes a complete example project with the exact tested Editor version.

## Connect

Call once from your game's startup code on Unity's main thread:

```csharp
using Nuxie.Unity;
using System.Threading.Tasks;

public static class GameServices
{
    public static async Task InitializeAsync()
    {
        await Nuxie.Client.ConfigureAsync(new NuxieOptions {
            IosApiKey = "YOUR_IOS_PUBLIC_KEY",
            AndroidApiKey = "YOUR_ANDROID_PUBLIC_KEY",
            Environment = NuxieEnvironment.Development
        });
    }
}
```

Use public platform keys from the same Nuxie app. Workspace secrets belong on your server. Production, warning logging, device locale and native billing are the defaults.

Inspector-first projects can create a **Nuxie Settings** asset and assign it to **Nuxie Bootstrap**. Both paths use the same client. Equivalent concurrent setup calls share initialization; conflicting options require explicit shutdown. Scene changes preserve the session. While attached, the callback dispatcher enables `Application.runInBackground` so native overlay callbacks can reach C#; shutdown restores the previous setting. Your game still owns pause, audio and input behavior.

Await startup from your own lifecycle and report exceptions in your game's error UI. Never block Unity's player thread with `.Wait()` or `.Result`.

## Know the player

```csharp
await Nuxie.Client.IdentifyAsync("player-123");
var identity = await Nuxie.Client.GetIdentityAsync();

// Sign out and rotate to a new anonymous identity.
await Nuxie.Client.ResetAsync();
```

Account changes invalidate observations from the previous customer. A delayed query for an old identity cannot become the new player's access.

`SetLocaleAsync("fr_FR")` overrides the locale; `SetLocaleAsync(null)` returns to the device setting. The change follows the native SDK's next profile synchronization. It does not promise an immediate refresh.

## Observe access without polling

```csharp
using System;
using Nuxie.Unity;
using UnityEngine;

public sealed class PremiumAccess : MonoBehaviour
{
    [SerializeField] private GameObject loading;
    [SerializeField] private GameObject premiumContent;
    private IDisposable subscription;

    private void OnEnable()
    {
        subscription = Nuxie.Client.ObserveFeature("premium", state => {
            loading.SetActive(state.State == FeatureStateKind.Unknown);
            premiumContent.SetActive(state.Access != null && state.Access.Allowed);
        });
    }

    private void OnDisable()
    {
        subscription?.Dispose();
        subscription = null;
    }
}
```

`ObserveFeature` immediately supplies the current immutable selection and then relevant updates on Unity's main thread. **Unknown** means no current profile has been admitted. **Reconciling** includes native purchase reconciliation. **Ready** is authoritative and can legitimately contain no access.

Use the optional **Nuxie Feature Gate** component for Inspector-configured Unknown, Allowed and Denied events. Disabling a component stops its observation; it does not shut down Nuxie.

## Query a specific entity

```csharp
var access = await Nuxie.Client.HasFeatureAsync("energy", new FeatureQuery {
    EntityId = "character-7",
    RequiredBalance = 1,
    Policy = FeatureQueryPolicy.Remote
});
```

Queries default to cache-first. Request remote authority explicitly when needed. An entity can use only its assigned grants; it does not borrow another entity's balance. Scoped results do not overwrite the global Feature snapshot.

## Spend once, retry safely

```csharp
var receipt = await Nuxie.Client.ConsumeFeatureAsync("energy", new FeatureCommand {
    Quantity = 1,
    EntityId = "character-7",
    OperationId = savedActionId
});

if (receipt.Accepted)
{
    // Execute or resume the game action associated with savedActionId.
}
```

Persist an operation ID with the game action and reuse it for retries. A new ID means a new spend. Native handles durable delivery; your game must also make its resulting action idempotent.

Check **Accepted**, not Active: spending the last unit can succeed while leaving zero balance and inactive access. The receipt preserves the original operation, customer, Feature, quantity, decision timestamp and replay indicator. Older native journal receipts can have a null timestamp. Quantities and required balances must be positive integers no greater than 2^53−1; balances remain doubles.

## Respond to authored actions

Subscribe with `Nuxie.Client.SubscribeAppAction(action => ...)`, and dispose the subscription when its owner is disabled. The action contains its authored name and payload plus Experience and Journey context. Use it to route to your own game UI or start application work.

`SubscribeActivity` delivers native lifecycle telemetry. `DismissAsync()` awaits native dismissal. Nuxie does not change `Time.timeScale`, mute audio or choose your input policy. The Lab tracks `screen_shown` / `screen_dismissed` activity and `revealing_screen_id` to preserve your existing pause state through a native screen stack.

## Choose who owns checkout

Native billing is the default. If your game already owns checkout, configure `Billing = NuxieBilling.External(controller)` with an `INuxiePurchaseController`.

The controller receives the exact selected store product/offer and returns a typed purchase outcome: Purchased, Cancelled, Pending or Failed. Restore returns Restored, NoPurchases or Failed. Nuxie owns callback deadlines and rejects late or duplicate completion. Starting checkout is not evidence of access; continue observing Features.

External billing leaves transaction finishing/acknowledgement to your host integration; Nuxie may still synchronize verified transactions according to the native provider authority. Native billing owns its transaction lifecycle.

Unity IAP is optional. Implement the controller around your existing store integration; preserve the exact selected offer and report pending purchases accurately. Do not run two checkout owners for the same Nuxie request.

## Try the SDK Lab

The complete project builds for iOS and Android and includes two scenes to demonstrate startup, scene transitions and subscriber cleanup.

Enter development keys, a customer, a metered Feature and two entities with disposable grants. **Run API Checks** performs real native/backend calls: identification, readiness, scoped query, one debit, same-ID retry, original receipt checks, entity isolation and reset/reidentify. Each fresh operation spends one unit.

Publish an Experience whose button emits an App Action. Trigger it, tap the actual native button, inspect the action log, dismiss, and verify game input/pause restoration. Simulator controls in the Editor are explicitly labelled simulated and are never counted as live validation.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Native call fails in Editor | Select the explicit simulator or run a mobile player. |
| Feature remains Unknown | Inspect setup/identity errors and profile synchronization; do not treat Unknown as a denial. |
| Trigger completes but no UI appears | Check the published trigger, audience and native activity log. Trigger completion is event acceptance. |
| Same operation appears twice | Reuse the original operation ID and make the game action idempotent. |
| Export fails | Use the qualified Unity/toolchain combination and inspect the package's preflight diagnostics. |
| Callback missing in Release | Inspect stripping/AOT diagnostics; qualify the release player, not only the Editor. |

Read the [API reference](Documentation~/api-reference.md), [native dependency guide](Documentation~/native-dependencies.md), [example setup](https://github.com/nuxieai/nuxie-unity/tree/main/ExampleProject), and [qualification record](Documentation~/testing-and-validation.md).
