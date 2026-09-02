using System.Collections.Concurrent;
using AzureMissionWorkspace.Application.Abstractions.AzureDevOps;

namespace AzureMissionWorkspace.Infrastructure.AzureDevOps;

/// <summary>
/// Deterministic fake Azure DevOps client used for local development and integration tests. It
/// simulates branch creation, artifact commits, pull requests, and pipeline queueing/status without
/// making any network calls, so the starter solution never requires a real Azure DevOps organization.
/// </summary>
public sealed class FakeAzureDevOpsClient : IAzureDevOpsClient, IRepositoryService, IPullRequestService, IPipelineService, IApprovalService, IArtifactService
{
    private readonly ConcurrentDictionary<int, PipelineRunStatus> _pipelineRuns = new();
    private readonly ConcurrentDictionary<string, MemoryStream> _artifacts = new();
    private int _nextPullRequestId = 1;
    private int _nextBuildId = 1000;

    public IRepositoryService Repositories => this;

    public IPullRequestService PullRequests => this;

    public IPipelineService Pipelines => this;

    public IApprovalService Approvals => this;

    public IArtifactService Artifacts => this;

    public Task<DeploymentBranch> CreateOrReuseDeploymentBranchAsync(string deploymentRequestId, CancellationToken cancellationToken = default)
    {
        var branchName = $"deployment-request/{deploymentRequestId}";
        return Task.FromResult(new DeploymentBranch(branchName, "main", Guid.NewGuid().ToString("N")));
    }

    public Task<string> CommitArtifactsAsync(DeploymentBranch branch, IReadOnlyDictionary<string, string> filePathToContent, string commitMessage, CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid().ToString("N"));

    public Task<int> CreatePullRequestAsync(DeploymentBranch branch, PullRequestSummary summary, CancellationToken cancellationToken = default)
        => Task.FromResult(Interlocked.Increment(ref _nextPullRequestId));

    public Task<int> QueueValidationPipelineAsync(string deploymentRequestId, string correlationId, CancellationToken cancellationToken = default)
        => QueuePipelineAsync();

    public Task<int> QueueDeploymentPipelineAsync(string deploymentRequestId, string correlationId, CancellationToken cancellationToken = default)
        => QueuePipelineAsync();

    private Task<int> QueuePipelineAsync()
    {
        var buildId = Interlocked.Increment(ref _nextBuildId);
        _pipelineRuns[buildId] = new PipelineRunStatus(buildId, "completed", "succeeded");
        return Task.FromResult(buildId);
    }

    public Task<PipelineRunStatus> GetStatusAsync(int buildId, CancellationToken cancellationToken = default)
    {
        if (_pipelineRuns.TryGetValue(buildId, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new PipelineRunStatus(buildId, "notFound", null));
    }

    public Task<bool> IsApprovalSatisfiedAsync(string deploymentRequestId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<Stream> DownloadArtifactAsync(int buildId, string artifactName, CancellationToken cancellationToken = default)
    {
        var key = $"{buildId}:{artifactName}";
        if (_artifacts.TryGetValue(key, out var stream))
        {
            var copy = new MemoryStream(stream.ToArray());
            return Task.FromResult<Stream>(copy);
        }

        return Task.FromResult<Stream>(new MemoryStream());
    }

    public async Task PublishArtifactAsync(int buildId, string artifactName, Stream content, CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _artifacts[$"{buildId}:{artifactName}"] = buffer;
    }
}
