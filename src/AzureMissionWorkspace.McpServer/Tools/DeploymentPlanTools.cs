using System.ComponentModel;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Application.UseCases.DeploymentPlans;
using AzureMissionWorkspace.McpServer.Dtos;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// MCP tools for generating and explaining the deterministic, normalized deployment plan produced
/// from an ARM what-if execution. The plan itself is authoritative; any explanation is a generated
/// summary layered on top and must never be treated as, or substituted for, the deterministic result.
/// </summary>
[McpServerToolType]
public sealed class DeploymentPlanTools
{
    private readonly GenerateDeploymentPlanHandler _generatePlan;
    private readonly IExplanationService _explanationService;
    private readonly IPlanAndPolicyCache _cache;

    public DeploymentPlanTools(GenerateDeploymentPlanHandler generatePlan, IExplanationService explanationService, IPlanAndPolicyCache cache)
    {
        _generatePlan = generatePlan;
        _explanationService = explanationService;
        _cache = cache;
    }

    [McpServerTool(Name = "generate_deployment_plan", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Mutating (planning only, never deploys). Executes an ARM what-if against the validated, compiled template and normalizes the result into a deterministic deployment plan.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentPlanDto> GenerateDeploymentPlanAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("The compiled ARM template JSON produced by validate_deployment_request.")] string compiledTemplateJson,
        [Description("The rendered non-secret parameters JSON produced by validate_deployment_request.")] string renderedParametersJson,
        CancellationToken cancellationToken)
    {
        var plan = await _generatePlan.HandleAsync(deploymentRequestId, compiledTemplateJson, renderedParametersJson, cancellationToken);
        _cache.SetPlan(deploymentRequestId, plan);
        return DeploymentPlanDto.FromDomain(plan);
    }

    [McpServerTool(Name = "get_deployment_plan", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Retrieves the most recently generated deployment plan for a deployment request.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public DeploymentPlanDto? GetDeploymentPlan(
        [Description("The deployment request id.")] Guid deploymentRequestId)
    {
        var plan = _cache.GetPlan(deploymentRequestId);
        return plan is null ? null : DeploymentPlanDto.FromDomain(plan);
    }

    [McpServerTool(Name = "explain_deployment_plan", ReadOnly = true, Idempotent = false)]
    [Description("Read-only. Produces a natural-language summary of the deterministic deployment plan. The summary is advisory explanation only; it never overrides the deterministic risk classification or change list.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<string> ExplainDeploymentPlanAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var plan = _cache.GetPlan(deploymentRequestId)
            ?? throw new KeyNotFoundException($"No deployment plan has been generated yet for deployment request '{deploymentRequestId}'.");
        return await _explanationService.ExplainDeploymentPlanAsync(plan, cancellationToken);
    }
}
