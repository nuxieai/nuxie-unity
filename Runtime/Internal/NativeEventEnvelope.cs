using System;
using System.Text.Json;

namespace Nuxie.Unity.Internal;

internal enum NativeEventType
{
  TriggerUpdate,
  FeatureAccessChanged,
  PurchaseRequest,
  RestoreRequest,
  FlowPresented,
  FlowDismissed,
  Unknown,
}

internal sealed class NativeEventEnvelope
{
  public required NativeEventType Type { get; init; }
  public string? RequestId { get; init; }
  public required long TimestampMs { get; init; }
  public required JsonElement Payload { get; init; }

  public static bool TryParse(string json, out NativeEventEnvelope? envelope, out string? error)
  {
    envelope = null;
    error = null;

    if (string.IsNullOrWhiteSpace(json))
    {
      error = "Native event payload was empty.";
      return false;
    }

    try
    {
      using var document = JsonDocument.Parse(json);
      var root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        error = "Native event payload must be a JSON object.";
        return false;
      }

      var typeRaw = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
      var type = typeRaw switch
      {
        "trigger_update" => NativeEventType.TriggerUpdate,
        "feature_access_changed" => NativeEventType.FeatureAccessChanged,
        "purchase_request" => NativeEventType.PurchaseRequest,
        "restore_request" => NativeEventType.RestoreRequest,
        "flow_presented" => NativeEventType.FlowPresented,
        "flow_dismissed" => NativeEventType.FlowDismissed,
        _ => NativeEventType.Unknown,
      };

      var requestId = root.TryGetProperty("requestId", out var requestIdElement)
        ? requestIdElement.GetString()
        : null;

      long timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
      if (root.TryGetProperty("timestampMs", out var timestampElement) &&
          timestampElement.ValueKind is JsonValueKind.Number &&
          timestampElement.TryGetInt64(out var parsedTimestamp))
      {
        timestampMs = parsedTimestamp;
      }

      var payload = root.TryGetProperty("payload", out var payloadElement)
        ? payloadElement.Clone()
        : root.Clone();

      envelope = new NativeEventEnvelope
      {
        Type = type,
        RequestId = requestId,
        TimestampMs = timestampMs,
        Payload = payload,
      };

      return true;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }
}
