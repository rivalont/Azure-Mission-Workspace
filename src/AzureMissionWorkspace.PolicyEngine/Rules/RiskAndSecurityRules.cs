using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.PolicyEngine.Abstractions;

namespace AzureMissionWorkspace.PolicyEngine.Rules;

/// <summary>Blocks changes classified as Delete or Replace unless the requestor has explicitly acknowledged the destructive impact.</summary>
public sealed class DestructiveChangeRestrictionPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-DESTRUCTIVE-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Plan is null)
        {
            return [];
        }

        return context.Plan.Changes
            .Where(c => c.ChangeType is PlanChangeType.Delete or PlanChangeType.Replace)
            .Select(change => new PolicyFinding(
                RuleId,
                "Destructive change detected",
                $"The deployment plan includes a {change.ChangeType} change to '{change.ResourceId}'.",
                PolicyFindingSeverity.Error,
                change.ResourceId,
                PropertyPath: null,
                ActualValue: change.ChangeType.ToString(),
                ExpectedCondition: "No Delete or Replace changes, or an elevated approval tier is applied.",
                Remediation: "Confirm the destructive change is intentional; elevated approval is required before deployment can proceed.",
                DocumentationReference: "docs/deployment-lifecycle.md"))
            .ToArray();
    }
}

/// <summary>Flags plan changes whose classification could not be determined, requiring manual review.</summary>
public sealed class UnknownChangeReviewPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-UNKNOWN-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Plan is null)
        {
            return [];
        }

        return context.Plan.Changes
            .Where(c => c.ChangeType == PlanChangeType.Unknown)
            .Select(change => new PolicyFinding(
                RuleId,
                "Unknown change requires manual review",
                $"The what-if result could not classify the change to '{change.ResourceId}'.",
                PolicyFindingSeverity.Warning,
                change.ResourceId,
                PropertyPath: null,
                ActualValue: "Unknown",
                ExpectedCondition: "All changes must be classified as Create, Modify, Delete, Replace, NoChange, or Ignore.",
                Remediation: "Review the raw what-if result manually before approving.",
                DocumentationReference: "docs/deployment-lifecycle.md"))
            .ToArray();
    }
}

/// <summary>Requires managed identity to be enabled where the service pattern's security controls declare it as required.</summary>
public sealed class ManagedIdentityRequirementPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-MI-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null)
        {
            return [];
        }

        if (!context.Request.Parameters.TryGetValue("managedIdentityEnabled", out var value) || value is null)
        {
            return [];
        }

        if (bool.TryParse(value, out var enabled) && enabled)
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Managed identity required",
                "This service pattern requires managed identity to be enabled.",
                PolicyFindingSeverity.Error,
                ResourceId: null,
                PropertyPath: "managedIdentityEnabled",
                ActualValue: value,
                ExpectedCondition: "true",
                Remediation: "Enable managed identity for the workload.",
                DocumentationReference: "docs/security-model.md"),
        ];
    }
}

/// <summary>Requires public network access to be disabled unless the environment profile explicitly permits it.</summary>
public sealed class PublicNetworkAccessPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-PNA-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null || !context.Request.Parameters.TryGetValue("publicNetworkAccess", out var value) || value is null)
        {
            return [];
        }

        if (string.Equals(value, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Public network access must be disabled",
                "Public network access is enabled but the platform baseline requires it to be disabled by default.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "publicNetworkAccess",
                ActualValue: value,
                ExpectedCondition: "Disabled",
                Remediation: "Set publicNetworkAccess to Disabled and use private endpoints or approved network integration instead.",
                DocumentationReference: "docs/security-model.md"),
        ];
    }
}

/// <summary>Requires diagnostic settings to be configured whenever the environment profile provides a Log Analytics workspace.</summary>
public sealed class DiagnosticSettingRequirementPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-DIAG-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null || !context.Request.Parameters.TryGetValue("logAnalyticsWorkspaceResourceId", out var value) || string.IsNullOrWhiteSpace(value))
        {
            return
            [
                new PolicyFinding(
                    RuleId,
                    "Diagnostic settings not configured",
                    "This service pattern requires a Log Analytics workspace reference for required diagnostic settings.",
                    PolicyFindingSeverity.Error,
                    ResourceId: null,
                    PropertyPath: "logAnalyticsWorkspaceResourceId",
                    ActualValue: null,
                    ExpectedCondition: "A non-empty Log Analytics workspace resource ID.",
                    Remediation: "Supply the environment profile's Log Analytics workspace resource ID.",
                    DocumentationReference: "docs/security-model.md"),
            ];
        }

        return [];
    }
}

/// <summary>Prevents any literal secret value from appearing in the non-secret parameter dictionary.</summary>
public sealed class SecretHandlingPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-SECRET-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null)
        {
            return [];
        }

        var findings = new List<PolicyFinding>();
        foreach (var secretInputName in context.Pattern.SecretInputs)
        {
            if (context.Request.Parameters.TryGetValue(secretInputName, out var value) && !string.IsNullOrEmpty(value) &&
                !value.Contains("://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith('$'))
            {
                findings.Add(new PolicyFinding(
                    RuleId,
                    "Secret input must be a reference, not a literal value",
                    $"Secret input '{secretInputName}' must reference a Key Vault secret identifier or pipeline secret variable, never a literal secret value.",
                    PolicyFindingSeverity.Blocking,
                    ResourceId: null,
                    PropertyPath: secretInputName,
                    ActualValue: "***redacted***",
                    ExpectedCondition: "A Key Vault URI (https://...) or pipeline variable reference ($(...)).",
                    Remediation: "Replace the literal value with a Key Vault secret identifier or pipeline secret variable reference.",
                    DocumentationReference: "docs/security-model.md"));
            }
        }

        return findings;
    }
}

/// <summary>Requires the requestor and any approver of a protected-environment deployment to be distinct identities.</summary>
public sealed class SeparationOfDutiesPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-SOD-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.EnvironmentProfile.EnvironmentType != EnvironmentType.Production)
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Separation of duties required for production",
                "Production deployment requests require an approver distinct from the requestor.",
                PolicyFindingSeverity.Info,
                ResourceId: null,
                PropertyPath: null,
                ActualValue: null,
                ExpectedCondition: "Approver.ObjectId != Requestor.ObjectId",
                Remediation: "No action needed; this finding documents an enforced control.",
                DocumentationReference: "docs/security-model.md"),
        ];
    }
}
