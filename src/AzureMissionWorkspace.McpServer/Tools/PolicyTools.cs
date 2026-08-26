using System.ComponentModel;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Application.UseCases.Policy;
using AzureMissionWorkspace.McpServer.Dtos;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// MCP tools for evaluating and explaining deterministic policy compliance. There is no generic
/// bypass or exception flag exposed here -- policy exceptions must be modeled as separate, expiring,
/// auditable records approved out of band by an authorized platform administrator.
/// </summary>
[McpServerToolType]
public sealed class PolicyTools
{
    private readonly EvaluatePolicyComplianceHandler _evaluate;
    private readonly IExplanationService _explanationService;
    private readonly IPlanAndPolicyCache _cache;

    public PolicyTools(EvaluatePolicyComplianceHandler evaluate, IExplanationService explanationService, IPlanAndPolicyCache cache)
    {
        _evaluate = evaluate;
        _explanationService = explanationService;
        _cache = cache;
    }

    [McpServerTool(Name = "evaluate_policy_compliance", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Mutating (evaluation only, never deploys or bypasses policy). Runs deterministic policy rules against the deployment request and its current deployment plan, if one has been generated.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<PolicyEvaluationDto> EvaluatePolicyComplianceAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var plan = _cache.GetPlan(deploymentRequestId);
        var evaluation = await _evaluate.HandleAsync(deploymentRequestId, plan, cancellationToken);
        _cache.SetPolicyEvaluation(deploymentRequestId, evaluation);
        return PolicyEvaluationDto.FromDomain(evaluation);
    }

    [McpServerTool(Name = "get_policy_findings", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Retrieves the most recent deterministic policy findings for a deployment request.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public PolicyEvaluationDto? GetPolicyFindings(
        [Description("The deployment request id.")] Guid deploymentRequestId)
    {
        var evaluation = _cache.GetPolicyEvaluation(deploymentRequestId);
        return evaluation is null ? null : PolicyEvaluationDto.FromDomain(evaluation);
    }

    [McpServerTool(Name = "explain_policy_finding", ReadOnly = true, Idempotent = false)]
    [Description("Read-only. Produces a natural-language explanation of a single deterministic policy finding, identified by its rule id, to help the requestor remediate it. The explanation never changes or bypasses the underlying finding.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<string> ExplainPolicyFindingAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("The ruleId of the finding to explain.")] string ruleId,
        CancellationToken cancellationToken)
    {
        var evaluation = _cache.GetPolicyEvaluation(deploymentRequestId)
            ?? throw new KeyNotFoundException($"No policy evaluation has been generated yet for deployment request '{deploymentRequestId}'.");

        var finding = evaluation.Findings.FirstOrDefault(f => string.Equals(f.RuleId, ruleId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Policy finding with ruleId '{ruleId}' was not found for this deployment request.");

        return await _explanationService.ExplainPolicyFindingAsync(finding, cancellationToken);
    }
}
