# Native Dependencies

The Unity package includes bridge code, but the platform projects must still resolve native Nuxie SDK dependencies.

## iOS

Bridge file: `Runtime/Plugins/iOS/NuxieUnityBridge.swift`

It imports `Nuxie` and exports:

- `NuxieUnity_Invoke(...)`
- `NuxieUnity_FreeCString(...)`

### Required

Add `nuxie-ios` to the generated Xcode project so `import Nuxie` resolves.

Typical approaches:

1. Add `nuxie-ios` as a Swift Package dependency in Xcode.
2. Link `Nuxie` product into Unity framework target(s).

If `Nuxie` is missing, iOS bridge calls return native error payloads.

## Android

Bridge files:

- `Runtime/Plugins/Android/nuxie-unity-bridge.androidlib/src/main/kotlin/io/nuxie/unity/NuxieUnityBridge.kt`
- `Runtime/Plugins/Android/nuxie-unity-bridge.androidlib/src/main/AndroidManifest.xml`

The bridge imports `io.nuxie.sdk.*`, so your Gradle build must include Android Nuxie SDK dependencies.

### Required

Ensure Unity's Android export resolves `io.nuxie.sdk` classes.

In Nuxie monorepo setups, this is typically wired as a project dependency on `:nuxie-android`.

## Unity Callback Host

Managed callback host: `Runtime/Unity/NuxieBridgeHost.cs`

- Auto-creates a persistent GameObject named `__NuxieBridgeHost`.
- Receives JSON event envelopes via `OnNuxieNativeEvent(string json)`.
- Dispatches to the runtime bridge parser.

## Event Envelope Contract

Native callbacks use a shared envelope:

```json
{
  "type": "trigger_update|feature_access_changed|purchase_request|restore_request|flow_presented|flow_dismissed",
  "requestId": "optional",
  "timestampMs": 1739246400000,
  "payload": {}
}
```

## Troubleshooting

### iOS: `No such module 'Nuxie'`

- Native iOS SDK is not linked into generated Xcode project.

### Android: `Unresolved reference: io.nuxie.sdk...`

- Android Nuxie SDK dependency is missing from Unity Gradle configuration.

### No callbacks in Unity

- Confirm `__NuxieBridgeHost` exists at runtime.
- Confirm platform bridge is invoking `UnitySendMessage("__NuxieBridgeHost", "OnNuxieNativeEvent", json)`.
