using AzureMissionWorkspace.Application.Abstractions.Policy;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.PolicyEngine.Abstractions;

namespace AzureMissionWorkspace.PolicyEngine;

/// <summary>Default <see cref="IPolicyEvaluator"/> implementation composing the static rule catalog and the result normalizer.</summary>
public sealed class DeterministicPolicyEvaluator : IPolicyEvaluator
{
    private readonly IPolicyCatalog _catalog;
    private readonly IPolicyResultNormalizer _normalizer;

    public DeterministicPolicyEvaluator(IPolicyCatalog catalog, IPolicyResultNormalizer normalizer)
    {
        _catalog = catalog;
        _normalizer = normalizer;
    }

    public Task<PolicyEvaluation> EvaluateAsync(DeploymentRequest request, ServicePattern pattern, EnvironmentProfile environmentProfile, DeploymentPlan? plan, CancellationToken cancellationToken = default)
    {
        var context = new PolicyRuleContext(request, pattern, environmentProfile, plan);
        var rules = _catalog.GetApplicableRules(context);

        var findings = rules.SelectMany(rule => rule.Evaluate(context)).ToArray();
        var evaluation = _normalizer.Normalize(request.Id.Value, findings, DateTimeOffset.UtcNow);

        return Task.FromResult(evaluation);
    }
}
