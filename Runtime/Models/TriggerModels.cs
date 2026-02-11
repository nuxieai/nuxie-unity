namespace Nuxie.Unity;

public enum TriggerUpdateKind
{
  Decision,
  Entitlement,
  Journey,
  Error,
}

public enum TriggerDecisionType
{
  NoMatch,
  Suppressed,
  JourneyStarted,
  JourneyResumed,
  FlowShown,
  AllowedImmediate,
  DeniedImmediate,
}

public enum SuppressReason
{
  AlreadyActive,
  ReentryLimited,
  Holdout,
  NoFlow,
  Unknown,
}

public enum EntitlementUpdateType
{
  Pending,
  Allowed,
  Denied,
}

public enum GateSource
{
  Cache,
  Purchase,
  Restore,
}

public enum JourneyExitReason
{
  Completed,
  GoalMet,
  TriggerUnmatched,
  Expired,
  Error,
  Cancelled,
}

public sealed class JourneyRef
{
  public required string JourneyId { get; init; }
  public required string CampaignId { get; init; }
  public string? FlowId { get; init; }
}

public sealed class TriggerDecision
{
  public required TriggerDecisionType Type { get; init; }
  public SuppressReason? Reason { get; init; }
  public string? RawReason { get; init; }
  public JourneyRef? Ref { get; init; }
}

public sealed class EntitlementUpdate
{
  public required EntitlementUpdateType Type { get; init; }
  public GateSource? Source { get; init; }
}

public sealed class JourneyUpdate
{
  public required string JourneyId { get; init; }
  public required string CampaignId { get; init; }
  public string? FlowId { get; init; }
  public required JourneyExitReason ExitReason { get; init; }
  public required bool GoalMet { get; init; }
  public long? GoalMetAtEpochMillis { get; init; }
  public double? DurationSeconds { get; init; }
  public string? FlowExitReason { get; init; }
}

public sealed class TriggerError
{
  public required string Code { get; init; }
  public required string Message { get; init; }
}

public sealed class TriggerUpdate
{
  public required TriggerUpdateKind Kind { get; init; }
  public TriggerDecision? Decision { get; init; }
  public EntitlementUpdate? Entitlement { get; init; }
  public JourneyUpdate? Journey { get; init; }
  public TriggerError? Error { get; init; }

  public bool IsTerminal => Internal.TriggerTerminalRules.IsTerminal(this);

  public static TriggerUpdate DecisionUpdate(TriggerDecision decision)
  {
    return new TriggerUpdate { Kind = TriggerUpdateKind.Decision, Decision = decision };
  }

  public static TriggerUpdate EntitlementUpdateItem(EntitlementUpdate entitlement)
  {
    return new TriggerUpdate { Kind = TriggerUpdateKind.Entitlement, Entitlement = entitlement };
  }

  public static TriggerUpdate JourneyUpdateItem(JourneyUpdate journey)
  {
    return new TriggerUpdate { Kind = TriggerUpdateKind.Journey, Journey = journey };
  }

  public static TriggerUpdate ErrorUpdate(TriggerError error)
  {
    return new TriggerUpdate { Kind = TriggerUpdateKind.Error, Error = error };
  }
}

public sealed class TriggerTerminalUpdate
{
  private TriggerTerminalUpdate(TriggerUpdate value)
  {
    Value = value;
  }

  public TriggerUpdate Value { get; }

  public TriggerUpdateKind Kind => Value.Kind;
  public TriggerDecision? Decision => Value.Decision;
  public EntitlementUpdate? Entitlement => Value.Entitlement;
  public JourneyUpdate? Journey => Value.Journey;
  public TriggerError? Error => Value.Error;

  public static TriggerTerminalUpdate From(TriggerUpdate update)
  {
    if (!update.IsTerminal)
    {
      throw new NuxieException("NON_TERMINAL_UPDATE", "Cannot convert non-terminal update to terminal update.");
    }

    return new TriggerTerminalUpdate(update);
  }
}

public sealed class TriggerUpdateEvent
{
  public required string RequestId { get; init; }
  public required TriggerUpdate Update { get; init; }
  public required bool IsTerminal { get; init; }
  public required long TimestampMs { get; init; }
}
