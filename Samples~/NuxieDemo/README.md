# Nuxie Demo Sample

This sample includes `Scripts/NuxieDemoController.cs`, a minimal integration component that demonstrates:

- `Nuxie.ConfigureAsync(...)`
- identity (`IdentifyAsync`)
- trigger lifecycle (`Trigger`, `OnUpdate`, terminal `Done`)
- flow presentation (`ShowFlowAsync`)
- profile refresh (`RefreshProfileAsync`)
- purchase/restore controller callbacks

## Scene Setup

1. Import the sample from Unity Package Manager.
2. Add an empty GameObject named `NuxieDemo`.
3. Attach `NuxieDemoController`.
4. Set `apiKey` (required), optional `distinctId`, `triggerEventName`, and `flowId`.
5. Enter Play Mode.

You can invoke sample actions from the component context menu:

- Initialize Nuxie
- Trigger Event
- Show Flow
- Refresh Profile
- Shutdown Nuxie

## Notes

- This sample is intentionally simple and logs through `Debug.Log`.
- Replace purchase/restore stubs with your real store implementation.
- Ensure native dependencies are wired before device builds.
