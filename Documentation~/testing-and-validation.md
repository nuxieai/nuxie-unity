# Qualification record

Validated on 13 September 2026 with Unity 6000.3.24f1, Xcode 26.5 and the exact native revisions in `NATIVE-PINS.json`.

| Check | Evidence |
| --- | --- |
| Managed contract | 20 passing .NET tests; .NET Standard 2.1 build with zero warnings/errors. |
| Unity Editor | Four passing EditMode tests, including both real scenes and native screen-stack pause restoration. |
| Unity player lifecycle | Nine passing PlayMode tests, including native-thread callback delivery, background-setting restoration, scene-independent client ownership and all three Feature Gate events disabling their component or GameObject during initial notification. |
| Package installation | Actual prepared UPM tarball resolved through Package Manager; both Unity test suites pass. |
| Native bridges | Swift compiled against the pinned real iOS SDK; Kotlin release AAR compiled against the pinned real Android SDK. |
| IL2CPP | iOS device, arm64 iOS simulator and Android arm64 exports pass. |
| Mobile applications | Xcode Debug simulator application and Gradle debug APK build, install and launch. |
| Android native packaging | Exactly one shared C++ runtime; APK zip alignment and every arm64 ELF LOAD segment pass 16 KiB checks. |
| Backend | Both mobile players pass identity/readiness, locale, cache-first versus remote entity access, unknown-entity denial, consumption, original-receipt replay, isolated balances and reset/re-identification. |
| Native UI | iOS rendered the authored Experience; tapping Continue delivered `sdk_lab_continue`, dismissed the screen and restored the Lab's original time scale/audio state. |

The live runs used disposable local development grants on an iPhone 17 Pro simulator and Android API 36 emulator. Each fresh check operation consumed one unit. Editor simulation was not counted as backend evidence.

Android native Experience interaction has not been visually qualified in this run; its emulator was headless. Store sandbox checkout, physical devices, distribution signing and release-player stripping remain separate release qualification. External billing has managed duplicate-callback/session-fencing tests and real native bridge compilation, not a completed store purchase claim.

From the SDK root:

```sh
dotnet test dotnet/Nuxie.Unity.slnx --nologo
dotnet build dotnet/Nuxie.Unity.Core/Nuxie.Unity.Core.csproj --nologo
python3 scripts/prepare-native.py
python3 scripts/check-ios.py
python3 scripts/check-unity.py --packed
python3 scripts/pack.py
```

`python3 scripts/check.py` runs the complete local SDK gate above. `node ../../scripts/pr-readiness.mjs run` records its PR readiness receipt after committing a clean branch. The Unity check requires a licensed Editor with iOS/Android modules and retains test XML and export logs under `.native/unity-check`. Set `UNITY_EDITOR` if the Editor is installed elsewhere. Native Android preparation requires Java 17 and Android SDK/NDK tooling.

The Unity check exports projects; building and running those native projects is an additional qualification step. Build the generated iOS simulator project with Xcode's `Unity-iPhone` scheme, Debug, `iphonesimulator`, arm64 and signing disabled. Build Android's generated `launcher` module with `assembleDebug` using the Gradle/JDK supplied by the tested Unity Editor. See the example and native dependency guides for local configuration and debug endpoint overrides.
