using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.PolicyEngine.Abstractions;

namespace AzureMissionWorkspace.PolicyEngine.Rules;

/// <summary>Blocks deployment to any Azure region not explicitly allowed by the environment profile.</summary>
public sealed class AllowedRegionsPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-REGION-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null || !context.Request.Parameters.TryGetValue("location", out var location) || location is null)
        {
            return [];
        }

        if (context.EnvironmentProfile.AllowedLocations.Contains(location, StringComparer.OrdinalIgnoreCase))
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Region not allowed",
                "The requested deployment location is not in the environment profile's allowed-locations list.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "location",
                ActualValue: location,
                ExpectedCondition: $"One of: {string.Join(", ", context.EnvironmentProfile.AllowedLocations)}",
                Remediation: "Select an allowed region for this environment profile.",
                DocumentationReference: "docs/security-model.md"),
        ];
    }
}

/// <summary>Requires every deployment request to carry the organization's standard tags.</summary>
public sealed class RequiredTagsPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-TAG-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        var findings = new List<PolicyFinding>();
        var parameterNames = context.Request.Parameters?.Values.Keys.ToArray() ?? [];

        foreach (var requiredTag in context.EnvironmentProfile.RequiredTags.Keys)
        {
            if (!parameterNames.Contains(requiredTag, StringComparer.OrdinalIgnoreCase))
            {
                findings.Add(new PolicyFinding(
                    RuleId,
                    "Missing required tag",
                    $"The deployment request is missing the organization-required tag '{requiredTag}'.",
                    PolicyFindingSeverity.Blocking,
                    ResourceId: null,
                    PropertyPath: requiredTag,
                    ActualValue: null,
                    ExpectedCondition: "Tag must be present.",
                    Remediation: $"Supply a value for the required tag '{requiredTag}'.",
                    DocumentationReference: "docs/service-pattern-authoring.md"));
            }
        }

        return findings;
    }
}

/// <summary>Enforces a deterministic naming convention for the primary workload name input.</summary>
public sealed class NamingConventionPolicyRule : IPolicyRule
{
    private static readonly System.Text.RegularExpressions.Regex WorkloadNamePattern = new("^[a-z][a-z0-9-]{2,23}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public string RuleId => "AMW-NAMING-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null || !context.Request.Parameters.TryGetValue("workloadName", out var workloadName) || workloadName is null)
        {
            return [];
        }

        if (WorkloadNamePattern.IsMatch(workloadName))
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Workload name violates naming convention",
                "workloadName must be lowercase alphanumeric with hyphens, 3-24 characters, starting with a letter.",
                PolicyFindingSeverity.Error,
                ResourceId: null,
                PropertyPath: "workloadName",
                ActualValue: workloadName,
                ExpectedCondition: WorkloadNamePattern.ToString(),
                Remediation: "Rename the workload to satisfy the naming convention.",
                DocumentationReference: "docs/service-pattern-authoring.md"),
        ];
    }
}

/// <summary>Blocks deployment requests for service patterns not present in the environment profile's allow-list.</summary>
public sealed class AllowedServicePatternsPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-PATTERN-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.EnvironmentProfile.AllowsServicePattern(context.Pattern.Id))
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Service pattern not allowed for environment profile",
                "The selected service pattern is not in this environment profile's allowed-service-patterns list.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "servicePatternId",
                ActualValue: context.Pattern.Id.Value,
                ExpectedCondition: "Service pattern must be present in allowedServicePatterns.",
                Remediation: "Select an approved service pattern for this environment profile, or request platform-administrator approval to extend the catalog.",
                DocumentationReference: "docs/service-pattern-authoring.md"),
        ];
    }
}

/// <summary>Ensures the target environment type is one the selected service pattern declares support for.</summary>
public sealed class AllowedEnvironmentTypesPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-ENV-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Pattern.SupportsEnvironmentType(context.EnvironmentProfile.EnvironmentType))
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Environment type not supported by service pattern",
                "The service pattern does not declare support for the target environment profile's environment type.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "environmentType",
                ActualValue: context.EnvironmentProfile.EnvironmentType.ToString(),
                ExpectedCondition: $"One of: {string.Join(", ", context.Pattern.SupportedEnvironmentTypes)}",
                Remediation: "Select a service pattern version that supports this environment type.",
                DocumentationReference: "docs/service-pattern-authoring.md"),
        ];
    }
}

/// <summary>Restricts deployment scope to resource-group or subscription scope for starter workload patterns.</summary>
public sealed class AllowedDeploymentScopePolicyRule : IPolicyRule
{
    public string RuleId => "AMW-SCOPE-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Pattern.Scope is DeploymentScope.ResourceGroup or DeploymentScope.Subscription)
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Deployment scope not permitted for workload patterns",
                "Starter workload service patterns are restricted to resource-group or subscription scope.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "scope",
                ActualValue: context.Pattern.Scope.ToString(),
                ExpectedCondition: "ResourceGroup or Subscription",
                Remediation: "Use a service pattern scoped to a resource group or subscription.",
                DocumentationReference: "docs/architecture.md"),
        ];
    }
}

/// <summary>Restricts data-classification-sensitive workloads from targeting non-production-hardened environment profiles.</summary>
public sealed class DataClassificationPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-DATA-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null || !context.Request.Parameters.TryGetValue("dataClassification", out var classification) || classification is null)
        {
            return [];
        }

        var isHighlyClassified = string.Equals(classification, nameof(DataClassification.HighlyConfidential), StringComparison.OrdinalIgnoreCase)
            || string.Equals(classification, nameof(DataClassification.Confidential), StringComparison.OrdinalIgnoreCase);

        if (!isHighlyClassified || context.EnvironmentProfile.EnvironmentType == EnvironmentType.Production)
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Confidential data classification requires a production-grade environment",
                "Confidential or highly confidential data classifications require a production environment profile with the associated policy baseline.",
                PolicyFindingSeverity.Warning,
                ResourceId: null,
                PropertyPath: "dataClassification",
                ActualValue: classification,
                ExpectedCondition: "environmentType == Production",
                Remediation: "Use a production environment profile, or reduce the declared data classification if accurate.",
                DocumentationReference: "docs/threat-model.md"),
        ];
    }
}
