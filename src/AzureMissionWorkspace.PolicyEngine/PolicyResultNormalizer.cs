using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.PolicyEngine.Abstractions;

namespace AzureMissionWorkspace.PolicyEngine;

/// <summary>Normalizes raw policy findings into an aggregate <see cref="PolicyEvaluation"/> with a fixed expiry window.</summary>
public sealed class PolicyResultNormalizer : IPolicyResultNormalizer
{
    private readonly TimeSpan _validityWindow;

    public PolicyResultNormalizer(TimeSpan? validityWindow = null)
    {
        _validityWindow = validityWindow ?? TimeSpan.FromHours(24);
    }

    public PolicyEvaluation Normalize(Guid deploymentRequestId, IReadOnlyCollection<PolicyFinding> findings, DateTimeOffset evaluatedAtUtc)
    {
        return new PolicyEvaluation(Guid.NewGuid(), deploymentRequestId, findings, evaluatedAtUtc, evaluatedAtUtc.Add(_validityWindow));
    }
}
