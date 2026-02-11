using System.Text.Json;
using Nuxie.Unity.Internal;

namespace Nuxie.Unity.Core.Tests;

public sealed class NativePayloadMapperTests
{
  [Fact]
  public void ParseProfileResponse_MapsKnownCollectionsAndAdditionalProperties()
  {
    using var document = JsonDocument.Parse(
      """
      {
        "customerId": "customer-123",
        "campaigns": [{ "id": "cmp-1", "name": "Starter" }],
        "segments": [{ "id": "seg-1", "name": "Beta" }],
        "flows": [{ "id": "flow-1" }],
        "features": [{ "id": "feat-1", "type": "metered", "balance": 3 }],
        "experiments": { "pricing": { "variantKey": "v2", "status": "assigned" } },
        "journeys": [{ "sessionId": "sess-1", "context": { "screen": "paywall", "count": 2 } }],
        "custom": { "nested": [1, true, "x"] }
      }
      """
    );

    var response = NativePayloadMapper.ParseProfileResponse(document.RootElement);

    Assert.Equal("customer-123", response.CustomerId);

    var campaigns = Assert.IsType<List<object?>>(response.Campaigns);
    var campaign = Assert.IsType<Dictionary<string, object?>>(Assert.Single(campaigns));
    Assert.Equal("cmp-1", campaign["id"]);

    Assert.True(response.AdditionalProperties.ContainsKey("experiments"));
    Assert.True(response.AdditionalProperties.ContainsKey("journeys"));

    var custom = Assert.IsType<Dictionary<string, object?>>(response.AdditionalProperties["custom"]);
    var nested = Assert.IsType<List<object?>>(custom["nested"]);
    Assert.Equal(1L, nested[0]);
    Assert.Equal(true, nested[1]);
    Assert.Equal("x", nested[2]);
  }

  [Fact]
  public void ParseFeatureCheckResult_MapsPreviewAndFeatureTypeAliases()
  {
    using var document = JsonDocument.Parse(
      """
      {
        "customerId": "customer-7",
        "featureId": "credits",
        "requiredBalance": 2,
        "code": "allowed",
        "allowed": true,
        "unlimited": false,
        "balance": 9,
        "type": "credit_system",
        "preview": {
          "hint": "upgrade",
          "options": ["basic", "pro"]
        }
      }
      """
    );

    var result = NativePayloadMapper.ParseFeatureCheckResult(document.RootElement);

    Assert.Equal("customer-7", result.CustomerId);
    Assert.Equal("credits", result.FeatureId);
    Assert.Equal(2, result.RequiredBalance);
    Assert.True(result.Allowed);
    Assert.Equal(9, result.Balance);
    Assert.Equal(FeatureType.CreditSystem, result.Type);

    var preview = Assert.IsType<Dictionary<string, object?>>(result.Preview);
    Assert.Equal("upgrade", preview["hint"]);
    var options = Assert.IsType<List<object?>>(preview["options"]);
    Assert.Equal("basic", options[0]);
    Assert.Equal("pro", options[1]);
  }

  [Fact]
  public void ParseFeatureUsageResult_MapsUsagePayload()
  {
    using var document = JsonDocument.Parse(
      """
      {
        "success": true,
        "featureId": "credits",
        "amountUsed": 1.5,
        "message": "ok",
        "usage": {
          "current": 4.5,
          "limit": 10,
          "remaining": 5.5
        }
      }
      """
    );

    var result = NativePayloadMapper.ParseFeatureUsageResult(document.RootElement);

    Assert.True(result.Success);
    Assert.Equal("credits", result.FeatureId);
    Assert.Equal(1.5, result.AmountUsed);
    Assert.Equal("ok", result.Message);
    Assert.NotNull(result.Usage);
    Assert.Equal(4.5, result.Usage!.Current);
    Assert.Equal(10, result.Usage.Limit);
    Assert.Equal(5.5, result.Usage.Remaining);
  }

  [Fact]
  public void ToDictionary_ConvertsNestedJsonScalarsArraysAndObjects()
  {
    using var document = JsonDocument.Parse(
      """
      {
        "count": 1,
        "nested": {
          "items": [false, null, 2.5]
        }
      }
      """
    );

    var dictionary = NativePayloadMapper.ToDictionary(document.RootElement);

    Assert.Equal(1L, dictionary["count"]);
    var nested = Assert.IsType<Dictionary<string, object?>>(dictionary["nested"]);
    var items = Assert.IsType<List<object?>>(nested["items"]);
    Assert.Equal(false, items[0]);
    Assert.Null(items[1]);
    Assert.Equal(2.5, items[2]);
  }
}
