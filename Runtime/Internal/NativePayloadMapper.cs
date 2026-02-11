using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Nuxie.Unity.Internal;

internal static class NativePayloadMapper
{
  internal static TriggerUpdate ParseTriggerUpdate(JsonElement payload)
  {
    var updateElement = payload;
    if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("update", out var nestedUpdate))
    {
      updateElement = nestedUpdate;
    }

    var kind = updateElement.TryGetProperty("kind", out var kindElement)
      ? kindElement.GetString()
      : null;

    return kind switch
    {
      "decision" when updateElement.TryGetProperty("decision", out var decisionElement) =>
        TriggerUpdate.DecisionUpdate(ParseDecision(decisionElement)),
      "entitlement" when updateElement.TryGetProperty("entitlement", out var entitlementElement) =>
        TriggerUpdate.EntitlementUpdateItem(ParseEntitlement(entitlementElement)),
      "journey" when updateElement.TryGetProperty("journey", out var journeyElement) =>
        TriggerUpdate.JourneyUpdateItem(ParseJourney(journeyElement)),
      "error" when updateElement.TryGetProperty("error", out var errorElement) =>
        TriggerUpdate.ErrorUpdate(ParseError(errorElement)),
      _ => TriggerUpdate.ErrorUpdate(new TriggerError
      {
        Code = "invalid_trigger_update",
        Message = $"Unknown trigger update kind '{kind ?? "null"}'.",
      }),
    };
  }

  internal static bool TryGetNativeTerminalFlag(JsonElement payload, out bool isTerminal)
  {
    isTerminal = false;
    if (payload.ValueKind != JsonValueKind.Object)
    {
      return false;
    }

    if (!payload.TryGetProperty("isTerminal", out var terminalElement))
    {
      return false;
    }

    if (terminalElement.ValueKind != JsonValueKind.True && terminalElement.ValueKind != JsonValueKind.False)
    {
      return false;
    }

    isTerminal = terminalElement.GetBoolean();
    return true;
  }

  internal static FeatureAccessChangedEvent ParseFeatureAccessChanged(NativeEventEnvelope envelope)
  {
    var payload = envelope.Payload;
    var featureId = payload.TryGetProperty("featureId", out var featureIdElement) ? featureIdElement.GetString() ?? "" : "";
    FeatureAccess? from = null;
    if (payload.TryGetProperty("from", out var fromElement) && fromElement.ValueKind == JsonValueKind.Object)
    {
      from = ParseFeatureAccess(fromElement);
    }

    var to = payload.TryGetProperty("to", out var toElement) && toElement.ValueKind == JsonValueKind.Object
      ? ParseFeatureAccess(toElement)
      : new FeatureAccess { Allowed = false, Unlimited = false, Type = FeatureType.Boolean };

    return new FeatureAccessChangedEvent
    {
      FeatureId = featureId,
      From = from,
      To = to,
      TimestampMs = envelope.TimestampMs,
    };
  }

  internal static PurchaseRequest ParsePurchaseRequest(NativeEventEnvelope envelope)
  {
    var payload = envelope.Payload;
    return new PurchaseRequest
    {
      RequestId = envelope.RequestId
        ?? (payload.TryGetProperty("requestId", out var requestIdElement) ? requestIdElement.GetString() : null)
        ?? "",
      Platform = payload.TryGetProperty("platform", out var platformElement) ? platformElement.GetString() ?? "unknown" : "unknown",
      ProductId = payload.TryGetProperty("productId", out var productElement) ? productElement.GetString() ?? "" : "",
      BasePlanId = payload.TryGetProperty("basePlanId", out var basePlanElement) ? basePlanElement.GetString() : null,
      OfferId = payload.TryGetProperty("offerId", out var offerElement) ? offerElement.GetString() : null,
      DisplayName = payload.TryGetProperty("displayName", out var displayNameElement) ? displayNameElement.GetString() : null,
      DisplayPrice = payload.TryGetProperty("displayPrice", out var displayPriceElement) ? displayPriceElement.GetString() : null,
      Price = payload.TryGetProperty("price", out var priceElement) && priceElement.TryGetDouble(out var price) ? price : null,
      CurrencyCode = payload.TryGetProperty("currencyCode", out var currencyElement) ? currencyElement.GetString() : null,
      TimestampMs = envelope.TimestampMs,
    };
  }

  internal static RestoreRequest ParseRestoreRequest(NativeEventEnvelope envelope)
  {
    var payload = envelope.Payload;
    return new RestoreRequest
    {
      RequestId = envelope.RequestId
        ?? (payload.TryGetProperty("requestId", out var requestIdElement) ? requestIdElement.GetString() : null)
        ?? "",
      Platform = payload.TryGetProperty("platform", out var platformElement) ? platformElement.GetString() ?? "unknown" : "unknown",
      TimestampMs = envelope.TimestampMs,
    };
  }

  internal static FlowLifecycleEvent ParseFlowLifecycleEvent(NativeEventEnvelope envelope)
  {
    var payload = envelope.Payload;
    var type = envelope.Type == NativeEventType.FlowPresented
      ? FlowLifecycleType.Presented
      : FlowLifecycleType.Dismissed;

    return new FlowLifecycleEvent
    {
      Type = type,
      FlowId = payload.TryGetProperty("flowId", out var flowIdElement) ? flowIdElement.GetString() : null,
      Reason = payload.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() : null,
      JourneyId = payload.TryGetProperty("journeyId", out var journeyIdElement) ? journeyIdElement.GetString() : null,
      CampaignId = payload.TryGetProperty("campaignId", out var campaignIdElement) ? campaignIdElement.GetString() : null,
      ScreenId = payload.TryGetProperty("screenId", out var screenIdElement) ? screenIdElement.GetString() : null,
      Error = payload.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null,
      TimestampMs = envelope.TimestampMs,
    };
  }

  private static TriggerDecision ParseDecision(JsonElement element)
  {
    var type = element.TryGetProperty("type", out var typeElement)
      ? typeElement.GetString()
      : null;

    return new TriggerDecision
    {
      Type = type switch
      {
        "no_match" => TriggerDecisionType.NoMatch,
        "suppressed" => TriggerDecisionType.Suppressed,
        "journey_started" => TriggerDecisionType.JourneyStarted,
        "journey_resumed" => TriggerDecisionType.JourneyResumed,
        "flow_shown" => TriggerDecisionType.FlowShown,
        "allowed_immediate" => TriggerDecisionType.AllowedImmediate,
        "denied_immediate" => TriggerDecisionType.DeniedImmediate,
        _ => TriggerDecisionType.NoMatch,
      },
      Reason = ParseSuppressReason(element),
      RawReason = element.TryGetProperty("rawReason", out var rawReasonElement) ? rawReasonElement.GetString() : null,
      Ref = element.TryGetProperty("ref", out var refElement) && refElement.ValueKind == JsonValueKind.Object
        ? ParseJourneyRef(refElement)
        : null,
    };
  }

  private static SuppressReason? ParseSuppressReason(JsonElement element)
  {
    if (!element.TryGetProperty("reason", out var reasonElement))
    {
      return null;
    }

    var reason = reasonElement.GetString();
    return reason switch
    {
      "already_active" => SuppressReason.AlreadyActive,
      "reentry_limited" => SuppressReason.ReentryLimited,
      "holdout" => SuppressReason.Holdout,
      "no_flow" => SuppressReason.NoFlow,
      "unknown" => SuppressReason.Unknown,
      _ => SuppressReason.Unknown,
    };
  }

  private static EntitlementUpdate ParseEntitlement(JsonElement element)
  {
    var type = element.TryGetProperty("type", out var typeElement)
      ? typeElement.GetString()
      : null;

    return new EntitlementUpdate
    {
      Type = type switch
      {
        "pending" => EntitlementUpdateType.Pending,
        "allowed" => EntitlementUpdateType.Allowed,
        "denied" => EntitlementUpdateType.Denied,
        _ => EntitlementUpdateType.Pending,
      },
      Source = ParseGateSource(element),
    };
  }

  private static GateSource? ParseGateSource(JsonElement element)
  {
    if (!element.TryGetProperty("source", out var sourceElement))
    {
      return null;
    }

    var source = sourceElement.GetString();
    return source switch
    {
      "cache" => GateSource.Cache,
      "purchase" => GateSource.Purchase,
      "restore" => GateSource.Restore,
      _ => GateSource.Cache,
    };
  }

  private static JourneyUpdate ParseJourney(JsonElement element)
  {
    return new JourneyUpdate
    {
      JourneyId = element.TryGetProperty("journeyId", out var journeyIdElement) ? journeyIdElement.GetString() ?? "" : "",
      CampaignId = element.TryGetProperty("campaignId", out var campaignIdElement) ? campaignIdElement.GetString() ?? "" : "",
      FlowId = element.TryGetProperty("flowId", out var flowIdElement) ? flowIdElement.GetString() : null,
      ExitReason = ParseJourneyExitReason(element),
      GoalMet = element.TryGetProperty("goalMet", out var goalMetElement) && goalMetElement.ValueKind == JsonValueKind.True,
      GoalMetAtEpochMillis = element.TryGetProperty("goalMetAtEpochMillis", out var goalMetAtElement) && goalMetAtElement.TryGetInt64(out var goalMetAt)
        ? goalMetAt
        : null,
      DurationSeconds = element.TryGetProperty("durationSeconds", out var durationElement) && durationElement.TryGetDouble(out var durationSeconds)
        ? durationSeconds
        : null,
      FlowExitReason = element.TryGetProperty("flowExitReason", out var flowExitElement) ? flowExitElement.GetString() : null,
    };
  }

  private static JourneyExitReason ParseJourneyExitReason(JsonElement element)
  {
    var value = element.TryGetProperty("exitReason", out var exitReasonElement)
      ? exitReasonElement.GetString()
      : null;

    return value switch
    {
      "completed" => JourneyExitReason.Completed,
      "goal_met" => JourneyExitReason.GoalMet,
      "trigger_unmatched" => JourneyExitReason.TriggerUnmatched,
      "expired" => JourneyExitReason.Expired,
      "error" => JourneyExitReason.Error,
      "cancelled" => JourneyExitReason.Cancelled,
      _ => JourneyExitReason.Completed,
    };
  }

  private static TriggerError ParseError(JsonElement element)
  {
    return new TriggerError
    {
      Code = element.TryGetProperty("code", out var codeElement) ? codeElement.GetString() ?? "trigger_failed" : "trigger_failed",
      Message = element.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "trigger_failed" : "trigger_failed",
    };
  }

  private static JourneyRef ParseJourneyRef(JsonElement element)
  {
    return new JourneyRef
    {
      JourneyId = element.TryGetProperty("journeyId", out var journeyIdElement) ? journeyIdElement.GetString() ?? "" : "",
      CampaignId = element.TryGetProperty("campaignId", out var campaignIdElement) ? campaignIdElement.GetString() ?? "" : "",
      FlowId = element.TryGetProperty("flowId", out var flowIdElement) ? flowIdElement.GetString() : null,
    };
  }

  internal static FeatureAccess ParseFeatureAccess(JsonElement element)
  {
    var type = element.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;

    return new FeatureAccess
    {
      Allowed = element.TryGetProperty("allowed", out var allowedElement) && allowedElement.ValueKind == JsonValueKind.True,
      Unlimited = element.TryGetProperty("unlimited", out var unlimitedElement) && unlimitedElement.ValueKind == JsonValueKind.True,
      Balance = element.TryGetProperty("balance", out var balanceElement) && balanceElement.TryGetInt32(out var balance)
        ? balance
        : null,
      Type = type switch
      {
        "metered" => FeatureType.Metered,
        "creditSystem" => FeatureType.CreditSystem,
        "credit_system" => FeatureType.CreditSystem,
        _ => FeatureType.Boolean,
      },
    };
  }

  internal static Dictionary<string, object?> PurchaseResultToDictionary(PurchaseResult result)
  {
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["type"] = result.Type switch
      {
        PurchaseResultType.Success => "success",
        PurchaseResultType.Cancelled => "cancelled",
        PurchaseResultType.Pending => "pending",
        PurchaseResultType.Failed => "failed",
        _ => "failed",
      },
    };

    if (result.Message is not null) payload["message"] = result.Message;
    if (result.ProductId is not null) payload["productId"] = result.ProductId;
    if (result.PurchaseToken is not null) payload["purchaseToken"] = result.PurchaseToken;
    if (result.OrderId is not null) payload["orderId"] = result.OrderId;
    if (result.TransactionId is not null) payload["transactionId"] = result.TransactionId;
    if (result.OriginalTransactionId is not null) payload["originalTransactionId"] = result.OriginalTransactionId;
    if (result.TransactionJws is not null) payload["transactionJws"] = result.TransactionJws;

    return payload;
  }

  internal static Dictionary<string, object?> RestoreResultToDictionary(RestoreResult result)
  {
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["type"] = result.Type switch
      {
        RestoreResultType.Success => "success",
        RestoreResultType.NoPurchases => "no_purchases",
        RestoreResultType.Failed => "failed",
        _ => "failed",
      },
    };

    if (result.RestoredCount.HasValue) payload["restoredCount"] = result.RestoredCount.Value;
    if (result.Message is not null) payload["message"] = result.Message;
    return payload;
  }

  internal static ProfileResponse ParseProfileResponse(JsonElement element)
  {
    var additional = new Dictionary<string, object?>(StringComparer.Ordinal);
    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (var property in element.EnumerateObject())
      {
        additional[property.Name] = JsonToObject(property.Value);
      }
    }

    return new ProfileResponse
    {
      CustomerId = element.TryGetProperty("customerId", out var customerIdElement) ? customerIdElement.GetString() : null,
      Campaigns = additional.TryGetValue("campaigns", out var campaigns) ? campaigns : null,
      Segments = additional.TryGetValue("segments", out var segments) ? segments : null,
      Flows = additional.TryGetValue("flows", out var flows) ? flows : null,
      Features = additional.TryGetValue("features", out var features) ? features : null,
      AdditionalProperties = additional,
    };
  }

  internal static FeatureCheckResult ParseFeatureCheckResult(JsonElement element)
  {
    return new FeatureCheckResult
    {
      CustomerId = element.TryGetProperty("customerId", out var customerIdElement) ? customerIdElement.GetString() ?? "" : "",
      FeatureId = element.TryGetProperty("featureId", out var featureIdElement) ? featureIdElement.GetString() ?? "" : "",
      RequiredBalance = element.TryGetProperty("requiredBalance", out var requiredBalanceElement) && requiredBalanceElement.TryGetInt32(out var requiredBalance)
        ? requiredBalance
        : 1,
      Code = element.TryGetProperty("code", out var codeElement) ? codeElement.GetString() ?? "" : "",
      Allowed = element.TryGetProperty("allowed", out var allowedElement) && allowedElement.ValueKind == JsonValueKind.True,
      Unlimited = element.TryGetProperty("unlimited", out var unlimitedElement) && unlimitedElement.ValueKind == JsonValueKind.True,
      Balance = element.TryGetProperty("balance", out var balanceElement) && balanceElement.TryGetInt32(out var balance)
        ? balance
        : null,
      Type = element.TryGetProperty("type", out var typeElement)
        ? ParseFeatureType(typeElement.GetString())
        : FeatureType.Boolean,
      Preview = element.TryGetProperty("preview", out var previewElement) ? JsonToObject(previewElement) : null,
    };
  }

  internal static FeatureUsageResult ParseFeatureUsageResult(JsonElement element)
  {
    FeatureUsageInfo? usage = null;
    if (element.TryGetProperty("usage", out var usageElement) && usageElement.ValueKind == JsonValueKind.Object)
    {
      usage = new FeatureUsageInfo
      {
        Current = usageElement.TryGetProperty("current", out var currentElement) && currentElement.TryGetDouble(out var current) ? current : 0,
        Limit = usageElement.TryGetProperty("limit", out var limitElement) && limitElement.TryGetDouble(out var limit) ? limit : null,
        Remaining = usageElement.TryGetProperty("remaining", out var remainingElement) && remainingElement.TryGetDouble(out var remaining) ? remaining : null,
      };
    }

    return new FeatureUsageResult
    {
      Success = element.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.True,
      FeatureId = element.TryGetProperty("featureId", out var featureIdElement) ? featureIdElement.GetString() ?? "" : "",
      AmountUsed = element.TryGetProperty("amountUsed", out var amountUsedElement) && amountUsedElement.TryGetDouble(out var amountUsed)
        ? amountUsed
        : 0,
      Message = element.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null,
      Usage = usage,
    };
  }

  internal static Dictionary<string, object?> ToDictionary(JsonElement element)
  {
    if (element.ValueKind != JsonValueKind.Object)
    {
      return new Dictionary<string, object?>();
    }

    var result = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var property in element.EnumerateObject())
    {
      result[property.Name] = JsonToObject(property.Value);
    }

    return result;
  }

  private static FeatureType ParseFeatureType(string? raw)
  {
    return raw switch
    {
      "metered" => FeatureType.Metered,
      "creditSystem" => FeatureType.CreditSystem,
      "credit_system" => FeatureType.CreditSystem,
      _ => FeatureType.Boolean,
    };
  }

  private static object? JsonToObject(JsonElement element)
  {
    return element.ValueKind switch
    {
      JsonValueKind.Null => null,
      JsonValueKind.True => true,
      JsonValueKind.False => false,
      JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
      JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
      JsonValueKind.String => element.GetString(),
      JsonValueKind.Array => JsonArrayToList(element),
      JsonValueKind.Object => ToDictionary(element),
      _ => element.ToString(),
    };
  }

  private static List<object?> JsonArrayToList(JsonElement element)
  {
    var list = new List<object?>();
    foreach (var item in element.EnumerateArray())
    {
      list.Add(JsonToObject(item));
    }

    return list;
  }
}
