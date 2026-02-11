namespace Nuxie.Unity;

public enum FeatureType
{
  Boolean,
  Metered,
  CreditSystem,
}

public sealed class FeatureAccess
{
  public bool Allowed { get; init; }
  public bool Unlimited { get; init; }
  public int? Balance { get; init; }
  public FeatureType Type { get; init; } = FeatureType.Boolean;
}

public sealed class FeatureAccessChangedEvent
{
  public required string FeatureId { get; init; }
  public FeatureAccess? From { get; init; }
  public required FeatureAccess To { get; init; }
  public required long TimestampMs { get; init; }
}

public sealed class FeatureCheckResult
{
  public required string CustomerId { get; init; }
  public required string FeatureId { get; init; }
  public required int RequiredBalance { get; init; }
  public required string Code { get; init; }
  public required bool Allowed { get; init; }
  public required bool Unlimited { get; init; }
  public int? Balance { get; init; }
  public required FeatureType Type { get; init; }
  public object? Preview { get; init; }
}

public sealed class FeatureUsageInfo
{
  public required double Current { get; init; }
  public double? Limit { get; init; }
  public double? Remaining { get; init; }
}

public sealed class FeatureUsageResult
{
  public required bool Success { get; init; }
  public required string FeatureId { get; init; }
  public required double AmountUsed { get; init; }
  public string? Message { get; init; }
  public FeatureUsageInfo? Usage { get; init; }
}
