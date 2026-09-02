using AzureMissionWorkspace.Domain.Enums;
using FluentAssertions;

namespace AzureMissionWorkspace.PolicyEngine.Tests;

public sealed class CompositionTests
{
    [Fact]
    public void StaticPolicyCatalog_returns_all_eighteen_rules()
    {
        var catalog = new AzureMissionWorkspace.PolicyEngine.StaticPolicyCatalog();
        var context = PolicyTestData.CreateContext();

        var rules = catalog.GetApplicableRules(context);

        rules.Should().HaveCount(18);
        rules.Select(static r => r.RuleId).Should().BeEquivalentTo(
        [
            "AMW-REGION-001",
            "AMW-TAG-001",
            "AMW-NAMING-001",
            "AMW-PATTERN-001",
            "AMW-ENV-001",
            "AMW-SCOPE-001",
            "AMW-DATA-001",
            "AMW-MODULE-001",
            "AMW-MODULE-VERSION-001",
            "AMW-SKU-001",
            "AMW-CLOUD-001",
            "AMW-PNA-001",
            "AMW-MI-001",
            "AMW-DIAG-001",
            "AMW-DESTRUCTIVE-001",
            "AMW-UNKNOWN-001",
            "AMW-SECRET-001",
            "AMW-SOD-001",
        ]);
    }

    [Fact]
    public void PolicyResultNormalizer_sets_future_expiration()
    {
        var evaluatedAt = DateTimeOffset.UtcNow;
        var normalizer = new AzureMissionWorkspace.PolicyEngine.PolicyResultNormalizer();

        var result = normalizer.Normalize(Guid.NewGuid(), [], evaluatedAt);

        result.ExpiresAtUtc.Should().BeAfter(result.EvaluatedAtUtc);
        result.ExpiresAtUtc.Should().BeOnOrAfter(evaluatedAt.AddHours(23.9));
    }

    [Fact]
    public async Task DeterministicPolicyEvaluator_composes_catalog_and_normalizer()
    {
        var evaluator = new AzureMissionWorkspace.PolicyEngine.DeterministicPolicyEvaluator(
            new AzureMissionWorkspace.PolicyEngine.StaticPolicyCatalog(),
            new AzureMissionWorkspace.PolicyEngine.PolicyResultNormalizer());

        var context = PolicyTestData.CreateContext(
            parameters: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["workloadName"] = "Bad_Name",
                ["location"] = "westus",
                ["environment"] = "dev",
                ["owner"] = "owner@example.com",
                ["dataClassification"] = "Confidential",
                ["managedIdentityEnabled"] = "false",
                ["publicNetworkAccess"] = "Enabled",
                ["logAnalyticsWorkspaceResourceId"] = "",
                ["apiSecret"] = "plain-text-secret",
            },
            environmentProfile: PolicyTestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Test));

        var result = await evaluator.EvaluateAsync(context.Request, context.Pattern, context.EnvironmentProfile, context.Plan);

        result.DeploymentRequestId.Should().Be(context.Request.Id.Value);
        result.Findings.Should().Contain(f => f.RuleId == "AMW-REGION-001");
        result.Findings.Should().Contain(f => f.RuleId == "AMW-NAMING-001");
        result.Findings.Should().Contain(f => f.RuleId == "AMW-SECRET-001");
        result.ExpiresAtUtc.Should().BeAfter(result.EvaluatedAtUtc);
    }
}
