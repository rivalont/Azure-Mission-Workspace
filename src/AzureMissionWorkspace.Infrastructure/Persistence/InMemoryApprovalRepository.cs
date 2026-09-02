using System.Collections.Concurrent;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Infrastructure.Persistence;

/// <summary>In-memory <see cref="IApprovalRepository"/> for local development and tests.</summary>
public sealed class InMemoryApprovalRepository : IApprovalRepository
{
    private readonly ConcurrentDictionary<Guid, List<ApprovalRequirement>> _requirements = new();
    private readonly ConcurrentDictionary<Guid, List<ApprovalDecision>> _decisions = new();

    public Task<IReadOnlyCollection<ApprovalRequirement>> GetRequirementsAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApprovalRequirement> result = _requirements.TryGetValue(deploymentRequestId.Value, out var list)
            ? list.ToArray()
            : [];

        return Task.FromResult(result);
    }

    public Task AddRequirementAsync(ApprovalRequirement requirement, CancellationToken cancellationToken = default)
    {
        var list = _requirements.GetOrAdd(requirement.DeploymentRequestId, _ => []);
        lock (list)
        {
            list.Add(requirement);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ApprovalDecision>> GetDecisionsAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApprovalDecision> result = _decisions.TryGetValue(deploymentRequestId.Value, out var list)
            ? list.ToArray()
            : [];

        return Task.FromResult(result);
    }

    public Task RecordDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        var list = _decisions.GetOrAdd(decision.DeploymentRequestId, _ => []);
        lock (list)
        {
            list.Add(decision);
        }

        return Task.CompletedTask;
    }
}
