namespace Nuxie.Unity.Internal;

internal static class TriggerTerminalRules
{
  internal static bool IsTerminal(TriggerUpdate update)
  {
    switch (update.Kind)
    {
      case TriggerUpdateKind.Error:
      case TriggerUpdateKind.Journey:
        return true;
      case TriggerUpdateKind.Decision:
        return update.Decision?.Type is TriggerDecisionType.AllowedImmediate
          or TriggerDecisionType.DeniedImmediate
          or TriggerDecisionType.NoMatch
          or TriggerDecisionType.Suppressed;
      case TriggerUpdateKind.Entitlement:
        return update.Entitlement?.Type is EntitlementUpdateType.Allowed or EntitlementUpdateType.Denied;
      default:
        return false;
    }
  }
}
