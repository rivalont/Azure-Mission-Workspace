using AzureMissionWorkspace.Domain.Entities;

namespace AzureMissionWorkspace.Application.Abstractions.Policy;

/// <summary>
/// Evaluates deterministic policy rules against a deployment request and, when available, its
/// normalized deployment plan. Implemented by the PolicyEngine project; the Application layer
/// depends only on this abstraction so orchestration logic stays independent of the concrete rule
/// catalog.
/// </summary>
public interface IPolicyEvaluator
{
    Task<PolicyEvaluation> EvaluateAsync(DeploymentRequest request, ServicePattern pattern, EnvironmentProfile environmentProfile, DeploymentPlan? plan, CancellationToken cancellationToken = default);
}
