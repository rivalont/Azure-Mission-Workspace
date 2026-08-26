using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Domain.Services;

/// <summary>
/// Deterministic rules for calculating the risk of an individual plan change and the overall risk
/// of a <see cref="DeploymentPlan"/>. The language model is never permitted to assign the
/// authoritative risk value -- only this calculator (or an equivalent deterministic policy rule)
/// may do so. Explanation services may summarize the result but must not alter it.
/// </summary>
public static class DeploymentRiskCalculator
{
    private static readonly HashSet<string> ElevatedReviewResourceTypeFragments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.Authorization",
        "Microsoft.KeyVault",
        "Microsoft.Network",
        "Microsoft.PolicyInsights",
    };

    public static DeploymentRisk CalculateChangeRisk(PlanChangeType changeType, string resourceType, EnvironmentType environmentType)
    {
        var touchesElevatedResource = ElevatedReviewResourceTypeFragments.Any(fragment => resourceType.StartsWith(fragment, StringComparison.OrdinalIgnoreCase));

        return changeType switch
        {
            PlanChangeType.Delete => DeploymentRisk.High,
            PlanChangeType.Replace => DeploymentRisk.High,
            PlanChangeType.Unknown => DeploymentRisk.ReviewRequired,
            PlanChangeType.Modify when touchesElevatedResource => DeploymentRisk.High,
            PlanChangeType.Modify when environmentType == EnvironmentType.Production => DeploymentRisk.Medium,
            PlanChangeType.Modify => DeploymentRisk.Low,
            PlanChangeType.Create when touchesElevatedResource => DeploymentRisk.Medium,
            PlanChangeType.Create when environmentType == EnvironmentType.Development => DeploymentRisk.Low,
            PlanChangeType.Create => DeploymentRisk.Low,
            PlanChangeType.NoChange => DeploymentRisk.Low,
            PlanChangeType.Ignore => DeploymentRisk.Low,
            _ => DeploymentRisk.ReviewRequired,
        };
    }

    /// <summary>Calculates the overall risk of a set of changes as the highest individual change risk, defaulting to Low for an empty set.</summary>
    public static DeploymentRisk CalculateOverallRisk(IReadOnlyCollection<DeploymentRisk> changeRisks)
    {
        if (changeRisks.Count == 0)
        {
            return DeploymentRisk.Low;
        }

        if (changeRisks.Contains(DeploymentRisk.ReviewRequired))
        {
            return DeploymentRisk.ReviewRequired;
        }

        if (changeRisks.Contains(DeploymentRisk.High))
        {
            return DeploymentRisk.High;
        }

        return changeRisks.Contains(DeploymentRisk.Medium) ? DeploymentRisk.Medium : DeploymentRisk.Low;
    }
}

/// <summary>A single required approval, calculated deterministically from a deployment plan's risk and the target environment.</summary>
public sealed record CalculatedApprovalRequirement(string RequiredRole, int RequiredApproverCount, bool RequiresDistinctFromRequestor);

/// <summary>
/// Deterministic rules for calculating which approvals are required for a deployment request,
/// based on overall risk and the target environment's protection level. Production (protected)
/// environments always require an approver distinct from the requestor.
/// </summary>
public static class ApprovalCalculator
{
    public static IReadOnlyCollection<CalculatedApprovalRequirement> Calculate(DeploymentRisk overallRisk, EnvironmentType environmentType, bool hasDestructiveChanges)
    {
        var isProtected = environmentType == EnvironmentType.Production;

        if (environmentType == EnvironmentType.Development && overallRisk == DeploymentRisk.Low && !hasDestructiveChanges)
        {
            // Create-only changes in a development environment may use a lower approval tier.
            return [new CalculatedApprovalRequirement("DeploymentApprover", RequiredApproverCount: 1, RequiresDistinctFromRequestor: false)];
        }

        var requiredApprovers = overallRisk switch
        {
            DeploymentRisk.ReviewRequired => 2,
            DeploymentRisk.High => 2,
            DeploymentRisk.Medium => isProtected ? 2 : 1,
            _ => 1,
        };

        return [new CalculatedApprovalRequirement("DeploymentApprover", requiredApprovers, RequiresDistinctFromRequestor: isProtected || overallRisk is DeploymentRisk.High or DeploymentRisk.ReviewRequired)];
    }
}
