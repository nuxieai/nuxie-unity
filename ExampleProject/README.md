# Unity SDK Lab

Open this directory in Unity 6000.3.24f1 after preparing the package with `python3 scripts/prepare-native.py` and `python3 scripts/pack.py` from the SDK root. It uses the prepared local UPM package. Both SdkLab and Gameplay scenes contain the same Lab UI so changing scenes exercises observer cleanup while retaining the SDK session.

The imported sample scripts are generated from `Samples~/SdkLab`; edit the sample, then run `python3 scripts/sync-example.py`. Do not edit the generated copy independently.

In Editor select **Use explicit Editor simulator** before connecting. Its visible banner means no backend/store calls are made. On a mobile player enter real public development platform keys, identify your disposable customer, and supply a Feature and two entity grants. Run API Checks spends one unit and retries the same operation ID. Keep that ID when deliberately retrying. New operation ID means a new spend.

Configure an authored `sdk_lab_requested` event and a button emitting `sdk_lab_continue`. Trigger it on-device, tap the native button, inspect App Action activity, dismiss and check gameplay pause/input restoration. Trigger completion alone does not validate presentation.

The package's preflight reports missing prepared native artifacts. The checked-in build entry points are `Nuxie.Unity.Example.LabBuild.Ios`, `.IosSimulator`, and `.Android`. The native development endpoint override is described in the native dependency guide. Do not commit platform keys or local port settings.

For local development, optionally create ignored `Assets/Resources/NuxieLab.local.json`:

```json
{
  "iosKey": "YOUR_IOS_PUBLIC_KEY",
  "androidKey": "YOUR_ANDROID_PUBLIC_KEY",
  "customer": "disposable-player",
  "feature": "credits",
  "entityA": "entity-a",
  "entityB": "entity-b",
  "trigger": "sdk_lab_requested",
  "autoConnect": true,
  "runApiChecks": false
}
```

Only Editor and development players load this file. `autoConnect` configures and identifies the mobile player on launch without running the spending checks. Setting `runApiChecks` to true automatically connects and spends one unit on every mobile launch; use disposable grants. The file is excluded from Git, but development builds contain its public keys. Never put server credentials here.

The example uses OpenGL ES 3 on Android for emulator compatibility and a windowed launch to avoid immersive-mode onboarding interrupting validation. Those choices are example settings, not SDK requirements. Development exports permit local networking; release exports do not add those development exceptions.
