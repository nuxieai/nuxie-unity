using System.Collections.Generic;

namespace Nuxie.Unity;

public sealed class TriggerOptions
{
  public IReadOnlyDictionary<string, object?>? Properties { get; init; }
  public IReadOnlyDictionary<string, object?>? UserProperties { get; init; }
  public IReadOnlyDictionary<string, object?>? UserPropertiesSetOnce { get; init; }

  internal Dictionary<string, object?> ToBridgePayload()
  {
    var payload = new Dictionary<string, object?>();

    if (Properties is not null)
    {
      payload["properties"] = Properties;
    }

    if (UserProperties is not null)
    {
      payload["userProperties"] = UserProperties;
    }

    if (UserPropertiesSetOnce is not null)
    {
      payload["userPropertiesSetOnce"] = UserPropertiesSetOnce;
    }

    return payload;
  }
}
