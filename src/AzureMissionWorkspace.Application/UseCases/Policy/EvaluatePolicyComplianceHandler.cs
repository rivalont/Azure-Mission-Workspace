using AzureMissionWorkspace.Application.Abstractions.Policy;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.UseCases.Policy;

/// <summary>
/// Use case: evaluate deterministic policy rules against a deployment request and its (optional)
/// normalized deployment plan. Blocking findings prevent progression to approval.
/// </summary>
public sealed class EvaluatePolicyComplianceHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IServicePatternRepository _patterns;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IPolicyEvaluator _policyEvaluator;

    public EvaluatePolicyComplianceHandler(
        IDeploymentRequestRepository requests,
        IServicePatternRepository patterns,
        IEnvironmentProfileRepository environmentProfiles,
        IPolicyEvaluator policyEvaluator)
    {
        _requests = requests;
        _patterns = patterns;
        _environmentProfiles = environmentProfiles;
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluation> HandleAsync(Guid deploymentRequestId, DeploymentPlan? plan, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");

        if (request.SelectedServicePatternId is null)
        {
            throw new InvalidOperationException("A service pattern must be selected before policy evaluation.");
        }

        var pattern = await _patterns.FindAsync(request.SelectedServicePatternId.Value, request.SelectedServicePatternVersion!.Value, cancellationToken)
            ?? throw new InvalidOperationException("The selected service pattern could not be re-resolved.");

        var profile = await _environmentProfiles.FindByIdAsync(request.EnvironmentProfileId, cancellationToken)
            ?? throw new InvalidOperationException("The referenced environment profile no longer exists.");

        return await _policyEvaluator.EvaluateAsync(request, pattern, profile, plan, cancellationToken);
    }
}
