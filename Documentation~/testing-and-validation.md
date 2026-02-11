# Testing and Validation

## Contract Tests (.NET)

Run from package root:

```bash
cd dotnet
dotnet test Nuxie.Unity.slnx --nologo
```

Current suite validates:

- Trigger terminal rule fixture parity.
- Trigger lifecycle and cancellation behavior.
- Purchase/restore request orchestration and timeout behavior.
- Runtime shutdown cleanup behavior.
- Native payload mapper parsing for profile/feature payloads.

## iOS Bridge Parse Check

```bash
swiftc -parse Runtime/Plugins/iOS/NuxieUnityBridge.swift
```

This validates Swift bridge syntax and exported C entrypoints parse cleanly.

## Android Build Validation

Android bridge compilation requires Unity Android export tooling and Android SDK on the build machine.

Validate by building an Android player after wiring `nuxie-android` dependency in Gradle.

## Recommended CI Matrix

1. .NET contract tests (`dotnet test`).
2. Swift parse check (`swiftc -parse`).
3. Unity batchmode compile check (editor script compile).
4. iOS and Android player build jobs.

## Manual Smoke Checklist

1. Configure with a valid API key.
2. Identify user and verify distinct ID accessors.
3. Trigger event and observe update stream + terminal completion.
4. Request feature access and usage.
5. Simulate purchase and restore callbacks.
6. Call shutdown and verify pending trigger cancellation.
