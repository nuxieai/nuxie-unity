# Nuxie Unity SDK Documentation

## What This Package Does

`com.nuxie.unity` is a native-first Unity wrapper for Nuxie.

The C# layer provides Unity-friendly APIs while iOS and Android behavior is executed by the native SDKs.

## Document Map

- [Getting Started](getting-started.md)
- [API Reference](api-reference.md)
- [Native Dependencies](native-dependencies.md)
- [Testing and Validation](testing-and-validation.md)

## Design Principles

- Keep business logic in native SDKs (parity with iOS/Android references).
- Keep Unity wrapper deterministic and thin.
- Keep bridge contracts aligned with RN/Flutter wrappers.

## Package Structure

- `Runtime/` C# API and models
- `Runtime/Internal/` bridge/event internals
- `Runtime/Plugins/iOS/` Swift bridge exports (`NuxieUnity_Invoke`)
- `Runtime/Plugins/Android/` Kotlin bridge object (`NuxieUnityBridge.invoke`)
- `Runtime/Unity/` callback host and Unity helpers
- `Samples~/NuxieDemo/` sample script and setup notes
