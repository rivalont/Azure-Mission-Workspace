using System.ComponentModel;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Application.UseCases.Approvals;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.McpServer.Dtos;
using AzureMissionWorkspace.McpServer.Security;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// MCP tools that submit deployment requests for approval and record human approval decisions.
/// Separation of duties for protected environments is enforced twice: once here, server-side, via
/// ASP.NET Core resource-based authorization (<see cref="AzureMissionWorkspace.McpServer.Authorization.DistinctApproverRequirementHandler"/>),
/// and again, independently, inside the Application layer's <see cref="RecordApprovalDecisionHandler"/>.
/// There is no MCP tool that lets a requestor approve their own request.
/// </summary>
[McpServerToolType]
public sealed class ApprovalTools
{
    private readonly SubmitDeploymentForApprovalHandler _submit;
    private readonly RecordApprovalDecisionHandler _recordDecision;
    private readonly IDeploymentRequestRepository _requests;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IPlanAndPolicyCache _cache;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentActorProvider _currentActor;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public ApprovalTools(
        SubmitDeploymentForApprovalHandler submit,
        RecordApprovalDecisionHandler recordDecision,
        IDeploymentRequestRepository requests,
        IEnvironmentProfileRepository environmentProfiles,
        IPlanAndPolicyCache cache,
        IAuthorizationService authorizationService,
        ICurrentActorProvider currentActor,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _submit = submit;
        _recordDecision = recordDecision;
        _requests = requests;
        _environmentProfiles = environmentProfiles;
        _cache = cache;
        _authorizationService = authorizationService;
        _currentActor = currentActor;
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool(Name = "submit_deployment_for_approval", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Mutating, approval-sensitive. Calculates the required approvals from the deterministic risk classification and moves the deployment request into AwaitingApproval. Never grants any approval itself.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<IReadOnlyCollection<ApprovalRequirementDto>> SubmitDeploymentForApprovalAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var plan = _cache.GetPlan(deploymentRequestId)
            ?? throw new KeyNotFoundException($"No deployment plan has been generated yet for deployment request '{deploymentRequestId}'.");

        var requirements = await _submit.HandleAsync(deploymentRequestId, plan, cancellationToken);
        return requirements.Select(ApprovalRequirementDto.FromDomain).ToArray();
    }

    [McpServerTool(Name = "record_approval_decision", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Mutating, approval-sensitive. Records a human approve/reject decision against a specific approval requirement. A requestor may never approve their own protected-environment deployment request -- this is enforced server-side and cannot be overridden by prompt instructions.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentApprover)]
    public async Task<DeploymentRequestDto> RecordApprovalDecisionAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("The approval requirement id being decided.")] Guid approvalRequirementId,
        [Description("'Approved' or 'Rejected'.")] ApprovalStatus decision,
        [Description("Optional reviewer comment. Must never contain secret values.")] string? comment,
        CancellationToken cancellationToken)
    {
        var request = await _requests.FindByIdAsync(new DeploymentRequestId(deploymentRequestId), cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");
        var profile = await _environmentProfiles.FindByIdAsync(request.EnvironmentProfileId, cancellationToken)
            ?? throw new InvalidOperationException("The referenced environment profile no longer exists.");

        var actor = _httpContextAccessor.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        var resource = new Authorization.ApprovalAuthorizationResource(request, profile);
        var authorizationResult = await _authorizationService.AuthorizeAsync(actor, resource, Application.Abstractions.Authorization.DistinctApproverRequirement.Instance);
        if (!authorizationResult.Succeeded)
        {
            throw new Domain.Exceptions.SeparationOfDutiesViolationException(
                "The requestor cannot approve their own deployment request for a protected environment or elevated-risk change.");
        }

        var input = new RecordApprovalDecisionInput(deploymentRequestId, approvalRequirementId, _currentActor.GetCurrentActor().ObjectId, decision, comment);
        var updated = await _recordDecision.HandleAsync(input, cancellationToken);
        return DeploymentRequestDto.FromDomain(updated);
    }
}
