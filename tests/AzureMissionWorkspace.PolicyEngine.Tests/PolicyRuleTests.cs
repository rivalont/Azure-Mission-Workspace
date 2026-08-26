using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.PolicyEngine.Abstractions;
using AzureMissionWorkspace.PolicyEngine.Rules;
using FluentAssertions;

namespace AzureMissionWorkspace.PolicyEngine.Tests;

public sealed class PolicyRuleTests
{
    public static IEnumerable<object[]> RuleCases()
    {
        yield return Case(
            new AllowedRegionsPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: With("location", "westus")),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new RequiredTagsPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: Without("costCenter")),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new NamingConventionPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: With("workloadName", "Bad_Name")),
            PolicyFindingSeverity.Error);

        yield return Case(
            new AllowedServicePatternsPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(environmentProfile: PolicyTestData.CreateEnvironmentProfile(allowedPatterns: ["storage-account"])),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new AllowedEnvironmentTypesPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(
                pattern: PolicyTestData.CreatePattern(supportedEnvironmentTypes: [EnvironmentType.Development]),
                environmentProfile: PolicyTestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Production)),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new AllowedDeploymentScopePolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(pattern: PolicyTestData.CreatePattern(scope: DeploymentScope.ManagementGroup)),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new DataClassificationPolicyRule(),
            PolicyTestData.CreateContext(parameters: With("dataClassification", "Internal")),
            PolicyTestData.CreateContext(
                parameters: With("dataClassification", "Confidential"),
                environmentProfile: PolicyTestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Test)),
            PolicyFindingSeverity.Warning);

        yield return Case(
            new ApprovedModuleSourcePolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(pattern: PolicyTestData.CreatePattern(moduleReferences: ["br:untrusted.azurecr.io/bicep/modules/app-service:1.0.0"])),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new PinnedModuleVersionPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(pattern: PolicyTestData.CreatePattern(moduleReferences: ["br:missionworkspace.azurecr.io/bicep/modules/app-service:latest"])),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new ApprovedSkuPolicyRule(),
            PolicyTestData.CreateContext(parameters: With("sku", "Standard_LRS")),
            PolicyTestData.CreateContext(parameters: With("sku", "Basic")),
            PolicyFindingSeverity.Error);

        yield return Case(
            new CloudCompatibilityPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(
                pattern: PolicyTestData.CreatePattern(supportedClouds: [AzureCloud.AzureCommercial]),
                environmentProfile: PolicyTestData.CreateEnvironmentProfile(cloud: AzureCloud.AzureGovernment)),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new PublicNetworkAccessPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: With("publicNetworkAccess", "Enabled")),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new ManagedIdentityRequirementPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: With("managedIdentityEnabled", "false")),
            PolicyFindingSeverity.Error);

        yield return Case(
            new DiagnosticSettingRequirementPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: With("logAnalyticsWorkspaceResourceId", "")),
            PolicyFindingSeverity.Error);

        yield return Case(
            new DestructiveChangeRestrictionPolicyRule(),
            PolicyTestData.CreateContext(plan: PolicyTestData.CreatePlan(Guid.NewGuid(), PlanChangeType.Create, DeploymentRisk.Low)),
            PolicyTestData.CreateContext(plan: PolicyTestData.CreatePlan(Guid.NewGuid(), PlanChangeType.Delete, DeploymentRisk.High)),
            PolicyFindingSeverity.Error);

        yield return Case(
            new UnknownChangeReviewPolicyRule(),
            PolicyTestData.CreateContext(plan: PolicyTestData.CreatePlan(Guid.NewGuid(), PlanChangeType.Create, DeploymentRisk.Low)),
            PolicyTestData.CreateContext(plan: PolicyTestData.CreatePlan(Guid.NewGuid(), PlanChangeType.Unknown, DeploymentRisk.ReviewRequired)),
            PolicyFindingSeverity.Warning);

        yield return Case(
            new SecretHandlingPolicyRule(),
            PolicyTestData.CreateContext(),
            PolicyTestData.CreateContext(parameters: With("apiSecret", "plain-text-secret")),
            PolicyFindingSeverity.Blocking);

        yield return Case(
            new SeparationOfDutiesPolicyRule(),
            PolicyTestData.CreateContext(environmentProfile: PolicyTestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Development)),
            PolicyTestData.CreateContext(environmentProfile: PolicyTestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Production)),
            PolicyFindingSeverity.Info);
    }

    [Theory]
    [MemberData(nameof(RuleCases))]
    public void Rule_distinguishes_compliant_and_violating_scenarios(IPolicyRule rule, PolicyRuleContext compliantContext, PolicyRuleContext violatingContext, PolicyFindingSeverity expectedSeverity)
    {
        var compliantFindings = rule.Evaluate(compliantContext);
        var violatingFindings = rule.Evaluate(violatingContext);

        compliantFindings.Should().BeEmpty();
        violatingFindings.Should().Contain(f => f.RuleId == rule.RuleId && f.Severity == expectedSeverity);
    }

    private static object[] Case(IPolicyRule rule, PolicyRuleContext compliantContext, PolicyRuleContext violatingContext, PolicyFindingSeverity expectedSeverity)
        => [rule, compliantContext, violatingContext, expectedSeverity];

    private static Dictionary<string, string> With(string key, string value)
    {
        var parameters = PolicyTestData.CreateCompliantParameters();
        parameters[key] = value;
        return parameters;
    }

    private static Dictionary<string, string> Without(string key)
    {
        var parameters = PolicyTestData.CreateCompliantParameters();
        parameters.Remove(key);
        return parameters;
    }
}
