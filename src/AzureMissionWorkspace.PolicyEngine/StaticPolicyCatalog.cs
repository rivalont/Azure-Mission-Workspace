using AzureMissionWorkspace.PolicyEngine.Abstractions;
using AzureMissionWorkspace.PolicyEngine.Rules;

namespace AzureMissionWorkspace.PolicyEngine;

/// <summary>
/// The starter deterministic policy catalog. All rules listed here run for every evaluation; more
/// advanced catalogs could filter by service pattern, cloud, or environment type.
/// </summary>
public sealed class StaticPolicyCatalog : IPolicyCatalog
{
    private static readonly IReadOnlyCollection<IPolicyRule> AllRules =
    [
        new AllowedRegionsPolicyRule(),
        new RequiredTagsPolicyRule(),
        new NamingConventionPolicyRule(),
        new AllowedServicePatternsPolicyRule(),
        new AllowedEnvironmentTypesPolicyRule(),
        new AllowedDeploymentScopePolicyRule(),
        new DataClassificationPolicyRule(),
        new ApprovedModuleSourcePolicyRule(),
        new PinnedModuleVersionPolicyRule(),
        new ApprovedSkuPolicyRule(),
        new CloudCompatibilityPolicyRule(),
        new PublicNetworkAccessPolicyRule(),
        new ManagedIdentityRequirementPolicyRule(),
        new DiagnosticSettingRequirementPolicyRule(),
        new DestructiveChangeRestrictionPolicyRule(),
        new UnknownChangeReviewPolicyRule(),
        new SecretHandlingPolicyRule(),
        new SeparationOfDutiesPolicyRule(),
    ];

    public IReadOnlyCollection<IPolicyRule> GetApplicableRules(PolicyRuleContext context) => AllRules;
}
