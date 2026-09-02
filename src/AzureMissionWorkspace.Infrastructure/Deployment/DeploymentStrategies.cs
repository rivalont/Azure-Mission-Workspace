using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Infrastructure.Deployment;

/// <summary>Deterministic fake standard ARM template deployment strategy for local development and tests.</summary>
public sealed class ArmTemplateDeploymentStrategy : IDeploymentStrategy
{
    public DeploymentStrategyType StrategyType => DeploymentStrategyType.ArmTemplate;

    public Task<DeploymentExecution> ExecuteAsync(DeploymentRequest request, ServicePattern pattern, CancellationToken cancellationToken = default)
    {
        var outputs = new Dictionary<string, string>
        {
            ["strategy"] = nameof(DeploymentStrategyType.ArmTemplate),
        };

        return Task.FromResult(new DeploymentExecution(Guid.NewGuid(), request.Id.Value, StrategyType, true, outputs, null, DateTimeOffset.UtcNow));
    }
}

/// <summary>Deterministic fake Azure Deployment Stacks strategy for local development and tests.</summary>
public sealed class DeploymentStackStrategy : IDeploymentStrategy
{
    public DeploymentStrategyType StrategyType => DeploymentStrategyType.DeploymentStack;

    public Task<DeploymentExecution> ExecuteAsync(DeploymentRequest request, ServicePattern pattern, CancellationToken cancellationToken = default)
    {
        var outputs = new Dictionary<string, string>
        {
            ["strategy"] = nameof(DeploymentStrategyType.DeploymentStack),
        };

        return Task.FromResult(new DeploymentExecution(Guid.NewGuid(), request.Id.Value, StrategyType, true, outputs, null, DateTimeOffset.UtcNow));
    }
}

/// <summary>
/// Dispatches deployment execution to the <see cref="IDeploymentStrategy"/> declared by the service
/// pattern. The starter implementation delegates to deterministic fake strategies; a production
/// deployment would substitute real ARM/Deployment Stacks adapters behind the same strategies.
/// </summary>
public sealed class DeploymentService : IDeploymentService
{
    private readonly IReadOnlyDictionary<DeploymentStrategyType, IDeploymentStrategy> _strategies;

    public DeploymentService(IEnumerable<IDeploymentStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.StrategyType);
    }

    public Task<DeploymentExecution> DeployAsync(DeploymentRequest request, ServicePattern pattern, CancellationToken cancellationToken = default)
    {
        if (!_strategies.TryGetValue(pattern.DeploymentStrategy, out var strategy))
        {
            throw new InvalidOperationException($"No deployment strategy registered for '{pattern.DeploymentStrategy}'.");
        }

        return strategy.ExecuteAsync(request, pattern, cancellationToken);
    }
}
