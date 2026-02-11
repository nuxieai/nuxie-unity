using System.Text.Json;
using Nuxie.Unity.Internal;

namespace Nuxie.Unity.Core.Tests;

public sealed class TriggerTerminalRulesFixtureTests
{
  [Fact]
  public void FixtureCasesRemainCanonical()
  {
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "trigger_terminal_cases.json");
    var fixtureJson = File.ReadAllText(fixturePath);
    var fixtures = JsonSerializer.Deserialize<List<FixtureCase>>(
      fixtureJson,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    Assert.NotEmpty(fixtures);

    foreach (var fixture in fixtures)
    {
      var update = BuildUpdate(fixture);
      var actual = TriggerTerminalRules.IsTerminal(update);
      Assert.Equal(fixture.ExpectedTerminal, actual);
    }
  }

  private static TriggerUpdate BuildUpdate(FixtureCase fixture)
  {
    return fixture.UpdateKind switch
    {
      "error" => TriggerUpdate.ErrorUpdate(new TriggerError { Code = "x", Message = "x" }),
      "journey" => TriggerUpdate.JourneyUpdateItem(new JourneyUpdate
      {
        JourneyId = "j1",
        CampaignId = "c1",
        ExitReason = JourneyExitReason.Completed,
        GoalMet = true,
      }),
      "decision" => TriggerUpdate.DecisionUpdate(new TriggerDecision
      {
        Type = fixture.DecisionKind switch
        {
          "allowedImmediate" => TriggerDecisionType.AllowedImmediate,
          "deniedImmediate" => TriggerDecisionType.DeniedImmediate,
          "noMatch" => TriggerDecisionType.NoMatch,
          "suppressed" => TriggerDecisionType.Suppressed,
          "flowShown" => TriggerDecisionType.FlowShown,
          "journeyStarted" => TriggerDecisionType.JourneyStarted,
          "journeyResumed" => TriggerDecisionType.JourneyResumed,
          _ => TriggerDecisionType.NoMatch,
        },
      }),
      "entitlement" => TriggerUpdate.EntitlementUpdateItem(new EntitlementUpdate
      {
        Type = fixture.EntitlementKind switch
        {
          "allowed" => EntitlementUpdateType.Allowed,
          "denied" => EntitlementUpdateType.Denied,
          _ => EntitlementUpdateType.Pending,
        },
      }),
      _ => TriggerUpdate.ErrorUpdate(new TriggerError { Code = "invalid", Message = "invalid fixture" }),
    };
  }

  private sealed class FixtureCase
  {
    public string Name { get; set; } = "";
    public string UpdateKind { get; set; } = "";
    public string? DecisionKind { get; set; }
    public string? EntitlementKind { get; set; }
    public bool ExpectedTerminal { get; set; }
  }
}
