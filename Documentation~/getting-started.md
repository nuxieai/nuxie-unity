# Getting Started

## 1. Add the Package

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.nuxie.unity": "https://github.com/nuxieio/nuxie-unity.git#main"
  }
}
```

## 2. Wire Native Dependencies

Before building for device, make sure platform projects include native Nuxie SDK dependencies.

- iOS: include `nuxie-ios` (for `import Nuxie` in Swift bridge).
- Android: include `nuxie-android` dependency (for `io.nuxie.sdk.*` in Kotlin bridge).

See [Native Dependencies](native-dependencies.md) for details.

## 3. Configure Nuxie Once

Create a bootstrap component:

```csharp
using System.Collections.Generic;
using Nuxie.Unity;
using UnityEngine;

public sealed class NuxieBootstrap : MonoBehaviour
{
  [SerializeField] private string apiKey = "NX_REPLACE_ME";

  private async void Start()
  {
    var sdk = await Nuxie.ConfigureAsync(new NuxieConfig(apiKey)
    {
      Environment = NuxieEnvironment.Production,
      LogLevel = NuxieLogLevel.Info,
      FlushAt = 20,
      FlushIntervalSeconds = 30,
    });

    await sdk.IdentifyAsync(
      "player_123",
      userProperties: new Dictionary<string, object?>
      {
        ["platform"] = "unity",
        ["build"] = Application.version,
      }
    );
  }
}
```

## 4. Trigger and Observe Updates

```csharp
var operation = Nuxie.Instance.Trigger("paywall_trigger");
var subscription = operation.OnUpdate(update => Debug.Log($"update={update.Kind}"));

var terminal = await operation.Done;
Debug.Log($"terminal={terminal.Kind}");

subscription.Dispose();
```

## 5. Feature and Profile APIs

```csharp
var profile = await Nuxie.Instance.RefreshProfileAsync();
var access = await Nuxie.Instance.HasFeatureAsync("premium");

if (access.Allowed)
{
  await Nuxie.Instance.UseFeatureAsync("credits", amount: 1);
}
```

## 6. Optional Purchase Controller

If flows request purchase/restore actions, pass an `INuxiePurchaseController` when configuring:

```csharp
var sdk = await Nuxie.ConfigureAsync(new NuxieConfig(apiKey), new MyPurchaseController());
```

See the sample and API reference for request/result models.
