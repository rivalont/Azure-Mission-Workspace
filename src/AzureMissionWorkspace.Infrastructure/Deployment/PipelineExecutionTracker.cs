using AzureMissionWorkspace.Domain.Entities;

namespace AzureMissionWorkspace.Infrastructure.Deployment;

/// <summary>
/// Tracks in-flight <see cref="PipelineExecution"/> records so that the MCP server (which queues
/// deployments) and the Worker (which reconciles their status) can share state without a full
/// persistence layer. This is presentation/orchestration-layer convenience state, not the system of
/// record -- the authoritative outcome is the finalized <see cref="DeploymentEvidence"/> package.
/// </summary>
public interface IPipelineExecutionTracker
{
    void Track(PipelineExecution execution);

    IReadOnlyCollection<PipelineExecution> ListActive();

    PipelineExecution? FindByDeploymentRequestId(Guid deploymentRequestId);
}

public sealed class InMemoryPipelineExecutionTracker : IPipelineExecutionTracker
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, PipelineExecution> _executionsByDeploymentRequestId = new();

    public void Track(PipelineExecution execution) => _executionsByDeploymentRequestId[execution.DeploymentRequestId] = execution;

    public IReadOnlyCollection<PipelineExecution> ListActive() => _executionsByDeploymentRequestId.Values
        .Where(e => e.Status is Domain.Enums.PipelineStatus.Queued or Domain.Enums.PipelineStatus.Running)
        .ToArray();

    public PipelineExecution? FindByDeploymentRequestId(Guid deploymentRequestId) => _executionsByDeploymentRequestId.GetValueOrDefault(deploymentRequestId);
}
