# Qualification record

## Native pin refresh — September 18, 2026

Current pins are iOS `072e38b24df67f7e6326815ed5e126c93c8e67d7` and Android
`1514b1cce3d64502b483c41fa551e7290caf10b0`, both pushed development commits.
They include shared decoder admission and hidden-screen media suspension.
`python3 scripts/prepare-native.py` rebuilt the exact Android SDK and bridge
Maven artifacts. `python3 scripts/check-ios.py` compiled the shipped Swift
bridge against the exact iOS pin, verified from its resolved checkout.
`python3 scripts/pack.py` verified native artifact hashes and packaged the UPM
archive. `python3 scripts/check-unity.py --packed` passed four EditMode and
nine PlayMode tests and exported iOS device, arm64 iOS simulator, and Android
arm64 IL2CPP projects. Both Xcode projects name the current iOS revision; the
exported Android bridge POM names the current Android revision. The source
project settings were restored after checking. The exported Android application
built with the Unity-supplied JDK/Gradle (56 tasks) and passed 16 KiB ZIP
alignment. Xcode 27 built the exported arm64 simulator application in Debug
with signing disabled. Signed-video playback and final readiness/review remain
outstanding for this refresh.


## Earlier video delivery candidate — September 18, 2026

The earlier candidate pinned pushed development revisions iOS
`38428e8bb1c65605d6c982ff22b2a18d63229950` and Android
`e76714a76e14b8f293e782934c107a789d0a67f0`, pending final native qualification
and review under [UNIV-3262](https://universe.basis.dev/issue/UNIV-3262).

Unity 6000.3.24f1 passed all 20 managed tests and the .NET Standard 2.1 build.
Native preparation built the exact Android Maven dependency and Kotlin bridge;
the Swift bridge compiled against the exact iOS pin on Xcode 27. The first
Swift check encountered a stale compiled C module after the native header
changed; cleaning that check project's generated products and rebuilding passed.

`scripts/check-unity.py --packed` installed the prepared UPM tarball, passed
four EditMode and nine PlayMode tests, and exported iOS device, arm64 iOS
simulator, and Android arm64 IL2CPP projects. The installed package's native
pins matched the candidate. Both exported Xcode projects contain the exact iOS
revision, and the exported Android bridge POM references the exact Android
revision. The source project's package manifest and settings were restored
after the check.

These results establish bridge compilation, managed behavior, package
installation, and project export. Building and running the exported native
applications with signed video, including acquisition and lifecycle checks,
remains outstanding. The earlier live evidence below does not qualify video
at these candidate revisions. Final readiness/review and the parent pointer
update remain pending.

## Earlier qualification

Validated on 13 September 2026 with Unity 6000.3.24f1 and Xcode 26.5,
before the video candidate pins above.

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
