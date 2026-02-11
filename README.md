# Nuxie Unity SDK

Native-first Unity wrapper for Nuxie.

This package keeps Unity business logic thin and delegates runtime behavior to the native Nuxie SDKs:

- iOS: `nuxie-ios`
- Android: `nuxie-android`

## Current Scope

- Unified C# API for configure, identity, trigger flows, features, profile, queue controls, and purchase callbacks.
- Native bridge transport for iOS (`DllImport("__Internal")`) and Android (`AndroidJavaClass`).
- Typed trigger/feature/purchase/profile models aligned with the React Native and Flutter wrappers.
- Contract tests (including trigger terminal fixtures and payload mapper tests).

## Requirements

- Unity `2022.3 LTS+`
- iOS `15+` and Android `minSdk 21+` (inherited from native SDKs)
- IL2CPP recommended for device builds

## Install

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.nuxie.unity": "https://github.com/nuxieio/nuxie-unity.git#main"
  }
}
```

In the Nuxie monorepo, this package is consumed as a git submodule at `/Users/levi/dev/nuxie-dev-5/packages/nuxie-unity`.

## Native Dependency Setup

The Unity bridge sources call into native Nuxie SDK classes. Your generated platform projects must include those native SDK dependencies.

### iOS

Add the `nuxie-ios` package/framework to the generated Xcode project so `import Nuxie` resolves for `Runtime/Plugins/iOS/NuxieUnityBridge.swift`.

### Android

Add the Android Nuxie SDK dependency to your Unity Gradle build so `io.nuxie.sdk.*` resolves for `Runtime/Plugins/Android/nuxie-unity-bridge.androidlib`.

In Nuxie monorepo-based builds, this is typically a Gradle project dependency on `:nuxie-android`.

## Quick Start

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
    });

    await sdk.IdentifyAsync(
      "player_123",
      userProperties: new Dictionary<string, object?>
      {
        ["plan"] = "free",
        ["platform"] = "unity",
      }
    );

    var operation = sdk.Trigger("paywall_trigger");
    operation.OnUpdate(update => Debug.Log($"Nuxie update: {update.Kind}"));

    var terminal = await operation.Done;
    Debug.Log($"Nuxie trigger terminal: {terminal.Kind}");
  }
}
```

## Purchase Controller

Provide an `INuxiePurchaseController` to handle purchase/restore requests emitted by native paywall flows:

```csharp
using System.Threading.Tasks;
using Nuxie.Unity;

public sealed class GamePurchaseController : INuxiePurchaseController
{
  public Task<PurchaseResult> OnPurchaseAsync(PurchaseRequest request)
  {
    // Call your store SDK here.
    return Task.FromResult(PurchaseResult.Failed("purchase_not_implemented"));
  }

  public Task<RestoreResult> OnRestoreAsync(RestoreRequest request)
  {
    return Task.FromResult(RestoreResult.NoPurchases());
  }
}
```

Pass this controller to `ConfigureAsync`.

## Repository Layout

- `Runtime/`: C# runtime API and models
- `Runtime/Internal/`: bridge contract, event parsing, terminal rules
- `Runtime/Plugins/iOS/`: Swift native bridge
- `Runtime/Plugins/Android/`: Kotlin native bridge (`.androidlib`)
- `Runtime/Unity/`: Unity host callback plumbing + coroutine helpers
- `Documentation~/`: package docs
- `Samples~/NuxieDemo`: integration sample
- `dotnet/`: .NET contract test harness

## Validation Commands

- `cd dotnet && dotnet test Nuxie.Unity.slnx --nologo`
- `swiftc -parse Runtime/Plugins/iOS/NuxieUnityBridge.swift`

Android native compilation requires an Android SDK + Unity Gradle export environment.

## Documentation

- [`Documentation~/index.md`](Documentation~/index.md)
- [`Documentation~/getting-started.md`](Documentation~/getting-started.md)
- [`Documentation~/api-reference.md`](Documentation~/api-reference.md)
- [`Documentation~/native-dependencies.md`](Documentation~/native-dependencies.md)
- [`Documentation~/testing-and-validation.md`](Documentation~/testing-and-validation.md)
