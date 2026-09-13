# Start a mobile integration

Use Unity 6000.3.24f1 with iOS and Android Build Support. Build IL2CPP arm64 players. Install the prepared UPM tarball or open `ExampleProject` after `python3 scripts/prepare-native.py` and `python3 scripts/pack.py`.

Call `await Nuxie.Client.ConfigureAsync(options)` once from the main thread with both public platform keys. Equivalent calls share setup. Identify the current player, observe Features, and trigger the event authored in your Nuxie app. The client survives scenes; dispose subscriptions in OnDisable. Explicit ShutdownAsync is for changing SDK configuration, not ordinary scene cleanup.

Inspector projects can use NuxieSettings, NuxieBootstrap, NuxieFeatureGate and NuxieTrigger. Subscribe errors or connect the components' Failed events to your error UI. Gate targets should be separate from the GameObject hosting the observer, so hiding a target does not disable its own observer.

The Editor intentionally rejects native setup. Select the Lab's explicit simulator to test application wiring without network/store operations. Device validation uses real development keys and real backend state.

Unity's main thread and the OS UI thread are different execution contexts. The package schedules native UI work correctly and delivers callbacks through its persistent player-thread dispatcher. Never block with Task.Wait or Task.Result. Call the public client from the Unity main thread.
