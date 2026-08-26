using AzureMissionWorkspace.Application.Abstractions.AzureDevOps;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.UseCases.Deployments;

/// <summary>
/// Use case: queue the Azure DevOps deployment pipeline for an approved deployment request. This
/// is the only path by which Azure Mission Workspace triggers an actual Azure deployment -- there
/// is no direct deployment path from the MCP server or the language model.
/// </summary>
public sealed class QueueDeploymentHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IAzureDevOpsClient _azureDevOps;

    public QueueDeploymentHandler(IDeploymentRequestRepository requests, IAzureDevOpsClient azureDevOps)
    {
        _requests = requests;
        _azureDevOps = azureDevOps;
    }

    public async Task<PipelineExecution> HandleAsync(Guid deploymentRequestId, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");

        if (request.Status != DeploymentRequestStatus.Approved)
        {
            throw new Domain.Exceptions.IllegalDeploymentRequestTransitionException(request.Status, DeploymentRequestStatus.DeploymentQueued);
        }

        if (!await _azureDevOps.Approvals.IsApprovalSatisfiedAsync(deploymentRequestId.ToString(), cancellationToken))
        {
            throw new InvalidOperationException("Azure DevOps approval gate has not been satisfied for this deployment request.");
        }

        var buildId = await _azureDevOps.Pipelines.QueueDeploymentPipelineAsync(deploymentRequestId.ToString(), request.CorrelationId.ToString(), cancellationToken);

        request.TransitionTo(DeploymentRequestStatus.DeploymentQueued, request.Requestor, request.Version);
        await _requests.SaveAsync(request, cancellationToken);

        return new PipelineExecution(Guid.NewGuid(), deploymentRequestId, "deploy.yml", buildId, PipelineStatus.Queued, DateTimeOffset.UtcNow);
    }
}

/// <summary>Use case: read the current status of a deployment request.</summary>
public sealed class GetDeploymentStatusHandler
{
    private readonly IDeploymentRequestRepository _requests;

    public GetDeploymentStatusHandler(IDeploymentRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<DeploymentRequest> HandleAsync(Guid deploymentRequestId, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        return await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");
    }
}

/// <summary>Use case: read the finalized evidence package for a completed deployment request, if available.</summary>
public sealed class GetDeploymentEvidenceHandler
{
    private readonly IDeploymentEvidenceRepository _evidence;

    public GetDeploymentEvidenceHandler(IDeploymentEvidenceRepository evidence)
    {
        _evidence = evidence;
    }

    public async Task<DeploymentEvidence?> HandleAsync(Guid deploymentRequestId, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        return await _evidence.FindByDeploymentRequestIdAsync(requestId, cancellationToken);
    }
}
