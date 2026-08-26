using System.Collections.Concurrent;
using AzureMissionWorkspace.Domain.Entities;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// Caches the most recently generated <see cref="DeploymentPlan"/> and <see cref="PolicyEvaluation"/>
/// per deployment request so that subsequent read-only tool calls (get/explain) can retrieve them
/// without re-running validation, what-if, or policy evaluation. This is presentation-layer
/// convenience state, not the system of record -- the authoritative evidence package is produced
/// and persisted separately once a deployment request reaches evidence finalization.
/// </summary>
public interface IPlanAndPolicyCache
{
    void SetPlan(Guid deploymentRequestId, DeploymentPlan plan);

    DeploymentPlan? GetPlan(Guid deploymentRequestId);

    void SetPolicyEvaluation(Guid deploymentRequestId, PolicyEvaluation evaluation);

    PolicyEvaluation? GetPolicyEvaluation(Guid deploymentRequestId);
}

public sealed class InMemoryPlanAndPolicyCache : IPlanAndPolicyCache
{
    private readonly ConcurrentDictionary<Guid, DeploymentPlan> _plans = new();
    private readonly ConcurrentDictionary<Guid, PolicyEvaluation> _policyEvaluations = new();

    public void SetPlan(Guid deploymentRequestId, DeploymentPlan plan) => _plans[deploymentRequestId] = plan;

    public DeploymentPlan? GetPlan(Guid deploymentRequestId) => _plans.GetValueOrDefault(deploymentRequestId);

    public void SetPolicyEvaluation(Guid deploymentRequestId, PolicyEvaluation evaluation) => _policyEvaluations[deploymentRequestId] = evaluation;

    public PolicyEvaluation? GetPolicyEvaluation(Guid deploymentRequestId) => _policyEvaluations.GetValueOrDefault(deploymentRequestId);
}
