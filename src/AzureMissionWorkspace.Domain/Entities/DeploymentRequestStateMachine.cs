using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Domain.Entities;

/// <summary>
/// Enforces legal <see cref="DeploymentRequestStatus"/> transitions for a
/// <see cref="DeploymentRequest"/>. This is the single source of truth for the deployment-request
/// lifecycle; it must not be bypassed by callers.
/// </summary>
public static class DeploymentRequestStateMachine
{
    private static readonly Dictionary<DeploymentRequestStatus, DeploymentRequestStatus[]> LegalTransitions = new()
    {
        [DeploymentRequestStatus.Draft] = [DeploymentRequestStatus.RequirementsComplete, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.RequirementsComplete] = [DeploymentRequestStatus.PatternSelected, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.PatternSelected] = [DeploymentRequestStatus.ParametersRendered, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.ParametersRendered] = [DeploymentRequestStatus.ValidationInProgress, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.ValidationInProgress] = [DeploymentRequestStatus.ValidationPassed, DeploymentRequestStatus.ValidationFailed, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.ValidationFailed] = [DeploymentRequestStatus.ParametersRendered, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.ValidationPassed] = [DeploymentRequestStatus.PlanGenerated, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.PlanGenerated] = [DeploymentRequestStatus.AwaitingApproval, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.AwaitingApproval] = [DeploymentRequestStatus.Approved, DeploymentRequestStatus.Rejected, DeploymentRequestStatus.Expired, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.Rejected] = [],
        [DeploymentRequestStatus.Approved] = [DeploymentRequestStatus.DeploymentQueued, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.DeploymentQueued] = [DeploymentRequestStatus.Deploying, DeploymentRequestStatus.Cancelled],
        [DeploymentRequestStatus.Deploying] = [DeploymentRequestStatus.Deployed, DeploymentRequestStatus.DeploymentFailed],
        [DeploymentRequestStatus.DeploymentFailed] = [DeploymentRequestStatus.EvidenceFinalized],
        [DeploymentRequestStatus.Deployed] = [DeploymentRequestStatus.EvidenceFinalized],
        [DeploymentRequestStatus.Cancelled] = [],
        [DeploymentRequestStatus.Expired] = [],
        [DeploymentRequestStatus.EvidenceFinalized] = [],
    };

    public static bool CanTransition(DeploymentRequestStatus from, DeploymentRequestStatus to)
    {
        return LegalTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static IReadOnlyCollection<DeploymentRequestStatus> GetLegalNextStates(DeploymentRequestStatus from)
    {
        return LegalTransitions.TryGetValue(from, out var allowed) ? allowed : [];
    }

    public static bool IsTerminal(DeploymentRequestStatus status)
    {
        return LegalTransitions.TryGetValue(status, out var allowed) && allowed.Length == 0;
    }
}
