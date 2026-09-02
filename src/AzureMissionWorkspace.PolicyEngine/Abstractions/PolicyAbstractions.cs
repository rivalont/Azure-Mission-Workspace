using AzureMissionWorkspace.Domain.Entities;

namespace AzureMissionWorkspace.PolicyEngine.Abstractions;

/// <summary>Context supplied to a policy rule for a single evaluation pass.</summary>
public sealed record PolicyRuleContext(
    DeploymentRequest Request,
    ServicePattern Pattern,
    EnvironmentProfile EnvironmentProfile,
    DeploymentPlan? Plan);

/// <summary>
/// A single deterministic policy rule. Rules never accept a generic bypass flag; exceptions to a
/// rule must be modeled as separate, expiring, auditable records requiring an authorized approver
/// (see <see cref="PolicyException"/>).
/// </summary>
public interface IPolicyRule
{
    string RuleId { get; }

    IReadOnlyCollection<PolicyFinding> Evaluate(PolicyRuleContext context);
}

/// <summary>Provides the set of policy rules applicable to a given evaluation context.</summary>
public interface IPolicyCatalog
{
    IReadOnlyCollection<IPolicyRule> GetApplicableRules(PolicyRuleContext context);
}

/// <summary>
/// Normalizes the individual findings produced by rule evaluation into the aggregate
/// <see cref="PolicyEvaluation"/> record, including expiry.
/// </summary>
public interface IPolicyResultNormalizer
{
    PolicyEvaluation Normalize(Guid deploymentRequestId, IReadOnlyCollection<PolicyFinding> findings, DateTimeOffset evaluatedAtUtc);
}

/// <summary>
/// A separate, expiring, auditable record granting a time-bound exception to a specific policy
/// rule for a specific deployment request. Exceptions must be created by an authorized approver
/// and are never implicit or self-granted.
/// </summary>
public sealed record PolicyException(
    Guid Id,
    string RuleId,
    Guid DeploymentRequestId,
    string ApprovedByObjectId,
    string Justification,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsActive(DateTimeOffset asOfUtc) => asOfUtc < ExpiresAtUtc;
}
