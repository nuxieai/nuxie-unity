namespace Nuxie.Unity;

public enum FlowLifecycleType
{
  Presented,
  Dismissed,
}

public sealed class FlowLifecycleEvent
{
  public required FlowLifecycleType Type { get; init; }
  public string? FlowId { get; init; }
  public string? Reason { get; init; }
  public string? JourneyId { get; init; }
  public string? CampaignId { get; init; }
  public string? ScreenId { get; init; }
  public string? Error { get; init; }
  public required long TimestampMs { get; init; }
}
