# Native dependencies and packaging

`NATIVE-PINS.json` is the source of truth. `python3 scripts/prepare-native.py` checks out the exact Android revision, builds the Nuxie AAR/POM, compiles the Unity bridge AAR, and stages the local Maven repository under ignored `Artifacts~/`. `python3 scripts/pack.py` verifies SHA-256 digests and creates a prepared UPM tarball. Native preparation requires Python 3, Git, Java 17 and Android SDK/NDK tooling. Consumers of the tarball do not compile Kotlin or clone the Android repository.

Android exports copy the prepared repository into the generated Gradle project and add one bridge dependency. POMs retain Kotlin, Billing and AndroidX transitive dependency resolution; the wrapper is not a fat AAR. The package does not replace Unity's Activity and supports the standard Activity/GameActivity host arrangement. Native lifecycle tracking owns subsequent Activity changes. Android runtime ABIs are arm64-v8a and x86_64; the example targets arm64.

iOS exports use PBXProject to attach the exact Nuxie Swift package to UnityFramework and compile the C ABI/Swift bridge. The hook also embeds the native `Nuxie_Nuxie.bundle` resources in UnityFramework. The package does not require CocoaPods or replace AppDelegate. Native callbacks copy UTF-8 into a player-thread queue; AOT roots and JNI keep rules protect Release builds.

Only generated export files are edited. Keep package metadata/GUIDs in source; do not commit local keys, generated Maven artifacts or Unity Library caches. Source checkout preparation and clean tarball installation are separate qualification steps.

Development endpoints are intentionally native-debug-only: `NUXIE_UNITY_API_ENDPOINT` launch environment on iOS and an Intent string extra of the same name on Android. Use the local backend's resolved port map. The public SDK does not expose a production endpoint override.

The Android export excludes Unity's `libc++_shared.so` from the unityLibrary module so the app packages the C++ runtime supplied by the pinned Nuxie AAR. This avoids duplicate native libraries and uses the runtime that Nuxie's native renderer was built against. Qualify your game's other native plugins with this shared runtime.

iOS Debug exports define Swift `DEBUG` for the bridge's local endpoint override. The example adds local-network access only for development exports; Android uses a debug-source-set manifest. These settings do not introduce a public endpoint override in the C# API.
