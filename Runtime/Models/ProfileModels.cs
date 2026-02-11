using System.Collections.Generic;

namespace Nuxie.Unity;

public sealed class ProfileResponse
{
  public string? CustomerId { get; init; }
  public object? Campaigns { get; init; }
  public object? Segments { get; init; }
  public object? Flows { get; init; }
  public object? Features { get; init; }
  public IReadOnlyDictionary<string, object?> AdditionalProperties { get; init; } = new Dictionary<string, object?>();
}
