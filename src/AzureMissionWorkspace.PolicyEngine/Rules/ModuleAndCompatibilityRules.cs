using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.PolicyEngine.Abstractions;

namespace AzureMissionWorkspace.PolicyEngine.Rules;

/// <summary>
/// Ensures every module reference declared by a service pattern originates from the environment
/// profile's approved private module registry (or an explicit relative path for local starter
/// examples), and is pinned to a specific semantic version rather than a mutable tag such as
/// <c>latest</c>.
/// </summary>
public sealed class ApprovedModuleSourcePolicyRule : IPolicyRule
{
    private const string ApprovedRegistryPrefix = "br:missionworkspace.azurecr.io/bicep/modules/";

    public string RuleId => "AMW-MODULE-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        return context.Pattern.ModuleReferences
            .Where(reference => !reference.StartsWith(ApprovedRegistryPrefix, StringComparison.OrdinalIgnoreCase)
                && !reference.StartsWith("./", StringComparison.Ordinal)
                && !reference.StartsWith("../", StringComparison.Ordinal))
            .Select(reference => new PolicyFinding(
                RuleId,
                "Module source not approved",
                $"The module reference '{reference}' is not from the approved private registry or an allowed relative path.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "moduleReferences",
                ActualValue: reference,
                ExpectedCondition: $"Starts with '{ApprovedRegistryPrefix}' or a relative path.",
                Remediation: "Update the service pattern to use the approved private module registry or an explicitly allowed relative path.",
                DocumentationReference: "docs/service-pattern-authoring.md"))
            .ToArray();
    }
}

/// <summary>Ensures module version references are pinned (not a mutable tag such as <c>latest</c>).</summary>
public sealed class PinnedModuleVersionPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-MODULE-VERSION-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        return context.Pattern.ModuleReferences
            .Where(static reference => !IsPinnedReference(reference))
            .Select(reference => new PolicyFinding(
                RuleId,
                "Module version not pinned",
                $"The module reference '{reference}' is not pinned to a specific semantic version.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "moduleReferences",
                ActualValue: reference,
                ExpectedCondition: "A semantic version such as 1.2.3; mutable tags like latest are not allowed.",
                Remediation: "Pin the module reference to an immutable semantic version.",
                DocumentationReference: "docs/service-pattern-authoring.md"))
            .ToArray();
    }

    private static bool IsPinnedReference(string reference)
    {
        if (reference.StartsWith("./", StringComparison.Ordinal) || reference.StartsWith("../", StringComparison.Ordinal))
        {
            return true;
        }

        var version = reference.Split(':').LastOrDefault();
        if (string.IsNullOrWhiteSpace(version) || string.Equals(version, "latest", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Version.TryParse(version, out _);
    }
}

/// <summary>Restricts a declared SKU input to an explicit allow-list, when the service pattern surfaces one.</summary>
public sealed class ApprovedSkuPolicyRule : IPolicyRule
{
    private static readonly HashSet<string> ApprovedSkus = new(StringComparer.OrdinalIgnoreCase)
    {
        "Standard_LRS", "Standard_ZRS", "Standard_GRS", "Standard_RAGRS",
        "P0v3", "P1v3", "P2v3", "S1", "S2",
        "standard", "premium",
    };

    public string RuleId => "AMW-SKU-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Request.Parameters is null)
        {
            return [];
        }

        var skuParameterNames = new[] { "replicationSku", "sku", "appServicePlanSku" };
        var findings = new List<PolicyFinding>();

        foreach (var parameterName in skuParameterNames)
        {
            if (context.Request.Parameters.TryGetValue(parameterName, out var value) && !string.IsNullOrEmpty(value) && !ApprovedSkus.Contains(value))
            {
                findings.Add(new PolicyFinding(
                    RuleId,
                    "SKU not approved",
                    $"The requested SKU '{value}' for '{parameterName}' is not on the approved SKU list.",
                    PolicyFindingSeverity.Error,
                    ResourceId: null,
                    PropertyPath: parameterName,
                    ActualValue: value,
                    ExpectedCondition: $"One of: {string.Join(", ", ApprovedSkus)}",
                    Remediation: "Select an approved SKU for this resource.",
                    DocumentationReference: "docs/service-pattern-authoring.md"));
            }
        }

        return findings;
    }
}

/// <summary>Blocks a deployment request targeting a cloud the selected service pattern does not declare support for.</summary>
public sealed class CloudCompatibilityPolicyRule : IPolicyRule
{
    public string RuleId => "AMW-CLOUD-001";

    public IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context)
    {
        if (context.Pattern.SupportsCloud(context.EnvironmentProfile.Cloud))
        {
            return [];
        }

        return
        [
            new PolicyFinding(
                RuleId,
                "Service pattern not compatible with target cloud",
                "The selected service pattern does not declare support for the environment profile's Azure cloud.",
                PolicyFindingSeverity.Blocking,
                ResourceId: null,
                PropertyPath: "cloud",
                ActualValue: context.EnvironmentProfile.Cloud.ToString(),
                ExpectedCondition: $"One of: {string.Join(", ", context.Pattern.SupportedClouds)}",
                Remediation: "Select a service pattern version that supports this cloud, or verify service availability using IServiceAvailabilityProvider before proceeding.",
                DocumentationReference: "docs/architecture.md"),
        ];
    }
}
