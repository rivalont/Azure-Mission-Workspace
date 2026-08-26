using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.Exceptions;
using AzureMissionWorkspace.Domain.Services;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.UseCases.Approvals;

/// <summary>
/// Use case: calculate and persist the approval requirements for a deployment request from its
/// deterministic risk classification, then move the request to AwaitingApproval.
/// </summary>
public sealed class SubmitDeploymentForApprovalHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IApprovalRepository _approvals;

    public SubmitDeploymentForApprovalHandler(IDeploymentRequestRepository requests, IEnvironmentProfileRepository environmentProfiles, IApprovalRepository approvals)
    {
        _requests = requests;
        _environmentProfiles = environmentProfiles;
        _approvals = approvals;
    }

    public async Task<IReadOnlyCollection<ApprovalRequirement>> HandleAsync(Guid deploymentRequestId, DeploymentPlan plan, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");

        var profile = await _environmentProfiles.FindByIdAsync(request.EnvironmentProfileId, cancellationToken)
            ?? throw new InvalidOperationException("The referenced environment profile no longer exists.");

        var calculated = ApprovalCalculator.Calculate(plan.OverallRisk, profile.EnvironmentType, plan.HasDestructiveChanges);

        var requirements = new List<ApprovalRequirement>();
        foreach (var item in calculated)
        {
            var requirement = new ApprovalRequirement(Guid.NewGuid(), deploymentRequestId, item.RequiredRole, item.RequiredApproverCount, item.RequiresDistinctFromRequestor);
            await _approvals.AddRequirementAsync(requirement, cancellationToken);
            requirements.Add(requirement);
        }

        request.TransitionTo(DeploymentRequestStatus.AwaitingApproval, request.Requestor, request.Version);
        await _requests.SaveAsync(request, cancellationToken);

        return requirements;
    }
}

/// <summary>
/// Use case: record a human approval or rejection decision. Enforces separation of duties -- a
/// requestor may never approve their own protected-environment (production, or otherwise
/// distinct-approver-required) deployment request.
/// </summary>
public sealed class RecordApprovalDecisionHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IApprovalRepository _approvals;

    public RecordApprovalDecisionHandler(IDeploymentRequestRepository requests, IApprovalRepository approvals)
    {
        _requests = requests;
        _approvals = approvals;
    }

    public async Task<DeploymentRequest> HandleAsync(RecordApprovalDecisionInput input, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(input.DeploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{input.DeploymentRequestId}' was not found.");

        var requirements = await _approvals.GetRequirementsAsync(requestId, cancellationToken);
        var requirement = requirements.FirstOrDefault(r => r.Id == input.ApprovalRequirementId)
            ?? throw new KeyNotFoundException($"Approval requirement '{input.ApprovalRequirementId}' was not found for this deployment request.");

        if (requirement.RequiresDistinctFromRequestor &&
            string.Equals(input.ApproverObjectId, request.Requestor.ObjectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new SeparationOfDutiesViolationException(
                "The requestor cannot approve their own deployment request for a protected environment or elevated-risk change.");
        }

        var decision = new ApprovalDecision(Guid.NewGuid(), input.DeploymentRequestId, input.ApprovalRequirementId, input.ApproverObjectId, input.Decision, input.Comment, DateTimeOffset.UtcNow);
        await _approvals.RecordDecisionAsync(decision, cancellationToken);

        if (input.Decision == ApprovalStatus.Rejected)
        {
            request.TransitionTo(DeploymentRequestStatus.Rejected, request.Requestor, request.Version);
            await _requests.SaveAsync(request, cancellationToken);
            return request;
        }

        var decisions = await _approvals.GetDecisionsAsync(requestId, cancellationToken);
        var allSatisfied = requirements.All(req =>
            decisions.Count(d => d.ApprovalRequirementId == req.Id && d.Status == ApprovalStatus.Approved) >= req.RequiredApproverCount);

        if (allSatisfied)
        {
            request.TransitionTo(DeploymentRequestStatus.Approved, request.Requestor, request.Version);
            await _requests.SaveAsync(request, cancellationToken);
        }

        return request;
    }
}
