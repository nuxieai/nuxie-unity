# Bazel worktree caches

The Bazel workspace imports `.bazel-cache.bazelrc`, matching `nuxie-runtime`
and all Nuxie SDKs. Action products, dependency downloads, and fetched
repository trees share `~/.cache/nuxie/bazel` across Git worktrees.

Bazel derives a separate output base from each checkout's path. Its `bazel-*`
links and build outputs remain specific to that checkout, while matching actions
can be restored from the shared cache.

Use `scripts/bazel/bazel.sh` to run the pinned Bazel version. Set an absolute
`NUXIE_BAZEL_CACHE_DIR` to relocate only the reusable caches. A shared
`NUXIE_BAZEL_OUTPUT_USER_ROOT` still contains separate output bases for each
checkout; an explicit `--output_base` must be unique to the checkout.

Verify the cache override without compiling native sources:

```sh
python3 -B -m unittest discover -s scripts/bazel -p 'test_cache.py'
```

This workspace establishes the cache policy for the SDK's Bazel migration.
Existing host-language build and qualification commands remain available.

## Direct SDK builds

```sh
python3 scripts/bazel/sdk.py build
python3 scripts/bazel/sdk.py test
python3 scripts/prepare-native.py
python3 scripts/check-ios.py
python3 scripts/check.py
```

`build` compiles the runtime C# sources for `netstandard2.1`; `test` compiles
and runs the existing 20 xUnit cases under .NET 8 with the official xUnit runner.
The Kotlin bridge and Swift bridge have direct `//:android_bridge_aar` and
`//:ios_bridge` targets. Native preparation preserves the UPM Maven coordinates,
release-component POM scopes, consumer rules and artifact checksums.

Native commands consume the exact `NATIVE-PINS.json` products. Supply absolute
`NUXIE_IOS_ARTIFACTS` / `NUXIE_ANDROID_ARTIFACTS` receipt paths to reuse parent
preparation, or let the helper prepare those revisions under `.native/`.
Every receipt is checked for source revision, committed source, paths, size and
SHA-256 before import. Packaging writes only checkout-local `Artifacts~/`;
Bazel compiler outputs and shared caches stay separate.

The full public `scripts/check.py` contract also runs the licensed Unity Editor,
EditMode/PlayMode behavior checks and mobile exports. Those engine checks require
Unity 6000.3.24f1 and the matching iOS/Android modules. The Buildkite pipeline uses
the same direct Bazel managed/native entry points; engine/player qualification
remains a separate installed-toolchain check.

NuGet dependency edges and versions come from the owning .NET lock files. NuGet
`contentHash` excludes signatures; `nuget-archives.json` separately pins the raw
ZIP SHA-512 that Bazel downloads. Refresh with
`python3 scripts/bazel/update-nuget.py --restore` using .NET 10+ for NuGet's
content-hash oracle, then use `--check` without any restore or compiler action.
Compilation itself uses pinned .NET SDK 8.0.415.

Buildkite sources `ci-cache.sh`, matching the root/runtime guarded agent cache.
Portable and Apple actions clear Android SDK variables; Android actions use an
isolated view of installed SDK 36/build-tools 36.0.0 with Java 21. Preparation of
the native SDK also requires its pinned NDK. `NUXIE_BAZEL_JOBS` limits actions;
CI uses two. Host Bazel rc files are disabled so they cannot override this policy.
