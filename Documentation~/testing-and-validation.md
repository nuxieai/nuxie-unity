# Qualification record

## Native v3 dependency qualification

This migration selects iOS
`28587b9b8e0c1fbcb012ebdff9645626706adcb8`
([iOS PR #488](https://github.com/nuxieai/nuxie-ios/pull/488)) and Android
`2e4c39929b397c808da0b9eefe4741aa83f8a4d0`
([Android PR #191](https://github.com/nuxieai/nuxie-android/pull/191)) for
[UNIV-4288](https://universe.basis.dev/issue/UNIV-4288).
`NATIVE-PINS.json` selects these inputs. Native product receipts must retain
their exact clean producer revisions; an earlier product cannot be relabeled.
Bazel compile actions share the existing host caches across worktrees, while
compiler outputs, staged products and fixture evidence remain checkout-owned.

The native v3 test target executes the shipped Swift adapter in a test-only,
scene-capable UIKit host against four reproducible, genuinely signed profiles
over a private loopback HTTP server. The host has no native SDK dependencies;
the test bundle links the actual adapter and SDK.
It requires accepted v3 admission, ordered adapter and native Journey events,
matching identities and completion outcome, and exact number/string/Boolean
run outputs. Retired v1/v2 schemas and a v3 descriptor signed under the retired
domain require a concrete native authentication failure after the identified
profile response completes. Live event transport and successful shutdown are
required for every case. Test results execute afresh even when compilation
reuses cached actions. The fixture generator, original native wire golden and
independent verifier are tracked alongside the harness.

These tests cover screenless native adapter sessions and their public event
boundary. They do not establish rendered form editing, native text-input UI,
Android JNI Journey execution, or an engine player. The historical native v2
results below do not qualify these v3 pins. Actual v3 gate counts, original
native receipts and prepared artifact hashes will be recorded separately after
the current native consumer checks complete.

### Selecting the local simulator

On this host, the identical compiled OSLog marker and capture command produced
an exact PID/timestamp event on iOS 26.5 and no events on the tested iOS 27
devices. This local runtime/device comparison is tracked in
[UNIV-4333](https://universe.basis.dev/issue/UNIV-4333). It does not establish an
Apple-wide defect. A separate same-binary comparison on the preserved iOS 26.5
device confirmed that the logic-test launcher's standalone `simctl spawn -s`
disables logging, while ordinary launch captures the marker live. The v3 target
therefore uses a declarative Bazel test host and retains the strict log oracle.
This launch fix does not qualify the earlier iOS 27 runtime/device observations.
The normal helper still chooses the latest available runtime; qualification on
this host explicitly selects the proven iOS 26.5 device.

Select an owned, already booted iOS 26.5 simulator using the existing caller
lease option before running the native gate:

```sh
NUXIE_IOS_SIMULATOR_ID=YOUR_BOOTED_DEVICE_UUID python3 scripts/check.py
```

The caller owns shutdown and preserves its device. The helper deletes only
simulators it creates itself. Authentication, PID/time scoping, signed-profile
verification and the event oracle remain unchanged. Qualification records must
include the exact selected UUID, runtime, command and original result.

## Previous native v2 Bazel build migration

The native v2 Bazel migration used iOS
`615a31bd03483e79a8590f8408b035659581f8f7`, landed in
[iOS PR #487](https://github.com/nuxieai/nuxie-ios/pull/487). Its Android
dependency was `7ebe2d72570b169712fd913fd2c5b2de4bf2848d`, landed in
[Android PR #189](https://github.com/nuxieai/nuxie-android/pull/189).
These pins and the results in this section belong to the previous native v2
build migration. They do not qualify the native v3 inputs above.

The owning SDK uses direct Bazel targets in local development and CI. Repository
and action caches are shared across worktrees; compiler outputs and staged
products are private to each checkout. At the landed producer pins above:

- `python3 -B -m unittest discover -s scripts/bazel -p 'test_*.py'`: all 42 frontend, cache, native-import and publication tests passed.
- `python3 scripts/bazel/update-nuget.py --check`: the 16 locked NuGet packages match the Bazel graph.
- `python3 scripts/check.py`: all 20 original xUnit cases, the managed build, Android preparation and Swift simulator bridge compilation passed. It then exited 1 at `check-unity.py --packed` because the licensed Editor is absent; the overall command is not reported as passing.
- `python3 scripts/pack.py`: native hashes, package inventory and Unity metadata passed, producing `com.nuxie.unity-0.2.0.tgz` (SHA-256 `23972928a3162db818803f7e4bfe026cc3318789ccff148135970974ad36f1eb`).
- `python3 scripts/bazel/sdk.py prepare-ios --configuration Debug` and the corresponding `Release` command passed with separate genuine producer receipts. Actual compiler actions use `arm64-apple-ios15.0`, `arm64-apple-ios15.0-simulator` and `x86_64-apple-ios15.0-simulator`; every binary's Mach-O platform, architecture inventory and six SDK resource files were independently verified.

Independent inspection of the final Android bridge AAR proves its API 23 floor,
owning bridge classes and exact consumer rules. All 25 published native Maven
files match the landed producer's receipt. The bridge POM preserves the original
Gradle scope/version contract, checked against the retained independent oracle.
Logs and inventories are under `.native/qualification/`.

The project owner waived licensed Unity Editor qualification for this migration.
EditMode/PlayMode tests, IL2CPP exports and rendered player acceptance remain
unqualified. The normal owning gate is unchanged. Historical qualification below
applies only to the revisions it names.

## Previous Experience goal and eligibility pin qualification

iOS `8c41617716c5c5086aba44e0d64f048692c515ae` and Android
`0cbe8086068eb1a0d7e1c53c4440de4c9e3bd5ae` implement the Experience policy
hard cut: one optional goal, retained conversion measurement, presentation-safe
exits, and offer-specific access checks. Both retain internal action/renderer
origin through capture and restart; ordinary analytics IDs cannot authorize
direct custom attribution. Milestone and old policy payloads are rejected.

Both native SDK full gates passed at these pins. On September 24, 2026,
`python3 scripts/check.py` passed all 20 managed tests, the core-library build,
Android preparation, and the Swift bridge simulator build. It then exited 1 at
`check-unity.py --packed`, reporting that `UNITY_EDITOR` must identify a licensed
Editor. Log: `/tmp/nuxie-goal-clock-unity-full.txt`.
`python3 scripts/pack.py` subsequently passed native hash, package inventory,
and Unity metadata validation and produced the UPM archive.

For this change, the project owner explicitly authorized delivery without
installing Unity. Editor EditMode/PlayMode tests, IL2CPP exports, and rendered
Unity-player acceptance were skipped under that exception. They are not claimed
as passing, and earlier engine results do not qualify these pins. The normal
`scripts/check.py` gate remains unchanged for future changes.

## Previous video SDK pin qualification

iOS `1e6970f306a9dac2ed567a239bf0e64a83e2d7cc` and Android
`20f9d42f7d5fe1cba6e2426d63c24499eb966ce7` include shared immutable-video
bindings, cancellation of obsolete acquisition, and live caption preference
refresh. Native Android preparation passed with all 50 artifact hashes verified.
The actual packed package passed five EditMode and nine PlayMode tests plus iOS
device/simulator and Android IL2CPP exports. Final committed-tree readiness is
recorded in the PR.

The real iOS simulator player passed audible and muted signed video with a
440 Hz engine tone. Screenshots contained red and blue video frames. The source
output peak was approximately 0.07071 before and after presentation; its sample
clock stayed fixed while the Lab paused game audio, then advanced 24,576 samples
following SDK dismissal. Each run uninstalled/reinstalled the synthetic Lab.
The probe exposed a Lab integration bug: Journey completion did not release its
saved game pause unless a separate screen-dismiss event arrived. The fix removes
only that Journey's presentations and restores the original time scale/audio
state when no active presentation remains; a two-Journey regression verifies
late completion cannot unpause another Journey. Both sample copies match.
This measures engine output and host ownership, not acoustic speaker latency or
other apps' audio. Earlier player measurements below retain their original pins.

## Earlier cache-preservation pin qualification

iOS `48fa51d6591f61d437620abfa06eb7fcb1a64564` preserves a verified cached object when a conflicting
signed byte-count claim is rejected. Android remains
`4d65783e2eec5b585673041146dff887258d3c93`. The Swift bridge compiles against this cache-only iOS fix. Final package
readiness is recorded in the pull request; the player measurements below name
the preceding iOS revision, with the same Android revision.

## Final video playback candidate checks

iOS `95d76d41eb4cc945cb57e5c1bcd8333ed15d55cc` and Android `4d65783e2eec5b585673041146dff887258d3c93` include
published Apple runtime 0.10.8 and Android runtime 0.4.8, rendered-video visibility,
and interruption recovery fixes. At these exact pins, `dotnet test
dotnet/Nuxie.Unity.slnx --nologo` passed all 20 managed tests and `dotnet build
dotnet/Nuxie.Unity.Core/Nuxie.Unity.Core.csproj --nologo` passed.
`python3 scripts/prepare-native.py` rebuilt the Android SDK and bridge Maven
artifacts, and `python3 scripts/pack.py` verified their hashes and produced the
UPM archive. `python3 scripts/check-unity.py --packed --tests-only` installed
that actual package and passed all four EditMode and nine PlayMode tests.
`python3 scripts/check-ios.py` also compiled the shipped Swift bridge against
the exact iOS pin for the ARM64 simulator. Packed-package iOS device, ARM64 simulator and Android IL2CPP exports passed.
The exported simulator player and Android APK built successfully. iOS playback
results follow; Android player measurements follow.

The actual ARM64 iOS simulator player passed a clean cold launch, background /
foreground return, and process restart while the delivery origin timed out.
Each 12-screenshot sequence observed at least three red/blue transitions after
startup, excluding a static launch snapshot as a playback oracle. Independently
hashed cached scene `242a0ebc242f2617d923a9e1e04a7cf01b9d5d6a8e62cc934f1f26df6efef4db`
and MP4 `f0a65563c100506c0f98c138e8be1ae333c9879bb77237fbada60bddcfa78669`
matched the signed inventory. This establishes the tested iOS playback and bounded
origin-outage cases, not audio measurement, caption accessibility, or every
resource/failure scenario. Canonical readiness passed at this candidate, including
all bridge and source checks. The actual Android player also passed clean cold playback, process restart and
Home/Recents return, with at least three video color changes per capture and
matching cached object hashes. Both mobile APKs passed 16 KiB ZIP alignment.
An earlier attempt to return by explicitly launching the singleTask game
Activity instead popped the native screen; that harness run is excluded.

## Native pin refresh — September 18, 2026

The preceding qualification used iOS `858321e2e57cc62b6cb978a97834ad068f748c02` and Android
`1514b1cce3d64502b483c41fa551e7290caf10b0`, both pushed development commits.
They include shared decoder admission and hidden-screen media suspension.
`python3 scripts/prepare-native.py` rebuilt the exact Android SDK and bridge
Maven artifacts. Before the cache-path fix, `python3 scripts/check-ios.py`
compiled the shipped Swift bridge against iOS `072e38b2`, and
`python3 scripts/pack.py` verified native hashes and packaged the UPM archive.
At those pins, `python3 scripts/check-unity.py --packed` passed four EditMode and
nine PlayMode tests and exported iOS device, arm64 iOS simulator, and Android
arm64 IL2CPP projects. Both Xcode projects name the current iOS revision; the
exported Android bridge POM names the current Android revision. The source
project settings were restored after checking. The exported Android application
built with the Unity-supplied JDK/Gradle (56 tasks) and passed 16 KiB ZIP
alignment. Xcode 27 built the exported arm64 simulator application in Debug
with signing disabled.

The Android arm64 Lab then consumed a synthetic canonical profile over HTTPS,
with normal authority headers and development-key signature verification. The
production compiler/publisher prepared the external-video release. The app
fetched the `.nux` and MP4 once each, emitted `journey_started` and `screen_shown`,
and rendered repeated red/blue video transitions. Sixteen pixel samples over
7.99 seconds observed both colors across repeated loops. After Home/foreground,
eight further samples over 3.49 seconds showed resumed red/blue transitions.
The request ledger still showed only one GET per asset. Independent hashes of
both content-addressed files in the app cache matched the release inventory.

This is emulator playback/acquisition evidence for the actual Unity player.
It does not qualify audio output, captions, or the full failure matrix. The
harness now returns the canonical profile and authority headers, acknowledges
the actual batch count, and preserves its descriptor across server restarts.

The iOS player initially exposed a native cache-path bug: AVFoundation rejected
the extension-free content-addressed MP4. Native revision `858321e2` fixes this
with a scoped `.mp4` symlink, retaining the cache lease without copying media.
Its regression failed before the fix; all 21 native video tests passed afterward.
The Unity simulator app rebuilt against that exact revision and passed the
original 12-screenshot red/blue playback probe. The updated development Lab
supports `autoConnect` without running spending checks; all 13 Unity tests and
three mobile exports passed with that change.

With the delivery server stopped and its HTTPS endpoint returning 502, both
apps were terminated and relaunched. Twelve iOS and ten Android screenshot
samples observed both video phases from cached profiles/assets. This qualifies
restart during a delivery-origin outage, not every airplane-mode, eviction,
or corruption scenario. iOS emitted an unbalanced appearance-transition warning;
its cause remains unisolated. The refreshed Swift bridge check resolved exact iOS `858321e2` and passed;
packing verified the native artifacts and produced the updated UPM archive.
Final readiness and review remain outstanding.


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

## Development game audio probe

Set `audioQualification: true` with `autoConnect: true` in the ignored
`NuxieLab.local.json` development configuration. The real AudioSource plays a
440 Hz tone before the native Experience. `NUXIE_GAME_AUDIO` logs require a
nonzero output buffer and progressing sample clock before presentation, a
stationary clock under the Lab's existing `AudioListener.pause` policy, and
restored output/progression after SDK dismissal. Use audible and muted signed
video fixtures and verify visible playback. This opt-in probe is compiled only
for development builds and the Editor; it does not measure external speaker
latency or competing applications. The Lab deliberately pauses game audio for
all presented screens, including muted ones. Remove the local flag afterward.
