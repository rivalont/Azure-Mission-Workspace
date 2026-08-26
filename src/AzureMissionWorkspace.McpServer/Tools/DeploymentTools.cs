using System.ComponentModel;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Application.UseCases.Deployments;
using AzureMissionWorkspace.McpServer.Dtos;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// MCP tools for queueing an approved deployment through Azure DevOps and reading back status and
/// evidence. There is no tool here, or anywhere in this server, that deploys directly from the MCP
/// server or the language model -- the Azure DevOps pipeline is the only deployment executor.
/// </summary>
[McpServerToolType]
public sealed class DeploymentTools
{
    private readonly QueueDeploymentHandler _queueDeployment;
    private readonly GetDeploymentStatusHandler _getStatus;
    private readonly GetDeploymentEvidenceHandler _getEvidence;

    public DeploymentTools(QueueDeploymentHandler queueDeployment, GetDeploymentStatusHandler getStatus, GetDeploymentEvidenceHandler getEvidence)
    {
        _queueDeployment = queueDeployment;
        _getStatus = getStatus;
        _getEvidence = getEvidence;
    }

    [McpServerTool(Name = "queue_deployment", ReadOnly = false, Destructive = true, Idempotent = false)]
    [Description("Deployment-triggering. Queues the Azure DevOps deployment pipeline for a fully approved deployment request. Fails unless the request is in the Approved status and the Azure DevOps approval gate is satisfied.")]
    [Authorize(Policy = AuthorizationPolicyNames.PlatformEngineer)]
    public async Task<PipelineExecutionDto> QueueDeploymentAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var execution = await _queueDeployment.HandleAsync(deploymentRequestId, cancellationToken);
        return PipelineExecutionDto.FromDomain(execution);
    }

    [McpServerTool(Name = "get_deployment_status", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Retrieves the current deployment-request status, including whether the deployment has been queued, is executing, failed, or completed.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentRequestDto> GetDeploymentStatusAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var request = await _getStatus.HandleAsync(deploymentRequestId, cancellationToken);
        return DeploymentRequestDto.FromDomain(request);
    }

    [McpServerTool(Name = "get_deployment_evidence", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Retrieves the finalized, hash-verifiable deployment evidence package for a completed deployment request, if evidence has been finalized. Secrets are never included.")]
    [Authorize(Policy = AuthorizationPolicyNames.Auditor)]
    public async Task<DeploymentEvidenceDto?> GetDeploymentEvidenceAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var evidence = await _getEvidence.HandleAsync(deploymentRequestId, cancellationToken);
        return evidence is null ? null : DeploymentEvidenceDto.FromDomain(evidence);
    }
}
