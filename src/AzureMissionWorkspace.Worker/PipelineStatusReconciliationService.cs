using AzureMissionWorkspace.Application.Abstractions.AzureDevOps;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.Infrastructure.Deployment;
using Microsoft.Extensions.Options;

namespace AzureMissionWorkspace.Worker;

public sealed class PipelineReconciliationOptions
{
    /// <summary>How often the Worker polls Azure DevOps for in-flight pipeline execution status.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(15);
}

/// <summary>
/// Polls Azure DevOps for the status of queued/running deployment pipelines, moves the
/// corresponding deployment request through Deploying -&gt; Deployed/DeploymentFailed as the pipeline
/// completes, and hands off to <see cref="EvidenceFinalizationService"/>-style finalization. The
/// Worker never executes a deployment itself -- it only observes the outcome of the Azure DevOps
/// pipeline that <c>queue_deployment</c> already queued.
/// </summary>
public sealed class PipelineStatusReconciliationService : BackgroundService
{
    private readonly IPipelineExecutionTracker _tracker;
    private readonly IPipelineService _pipelines;
    private readonly IDeploymentRequestRepository _requests;
    private readonly DeploymentEvidenceFinalizer _evidenceFinalizer;
    private readonly ILogger<PipelineStatusReconciliationService> _logger;
    private readonly TimeSpan _pollInterval;

    public PipelineStatusReconciliationService(
        IPipelineExecutionTracker tracker,
        IPipelineService pipelines,
        IDeploymentRequestRepository requests,
        DeploymentEvidenceFinalizer evidenceFinalizer,
        IOptions<PipelineReconciliationOptions> options,
        ILogger<PipelineStatusReconciliationService> logger)
    {
        _tracker = tracker;
        _pipelines = pipelines;
        _requests = requests;
        _evidenceFinalizer = evidenceFinalizer;
        _logger = logger;
        _pollInterval = options.Value.PollInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var execution in _tracker.ListActive())
            {
                await ReconcileAsync(execution, stoppingToken);
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReconcileAsync(Domain.Entities.PipelineExecution execution, CancellationToken cancellationToken)
    {
        if (execution.BuildId is null)
        {
            return;
        }

        var runStatus = await _pipelines.GetStatusAsync(execution.BuildId.Value, cancellationToken);
        var mapped = MapStatus(runStatus.StatusName, runStatus.Result);
        if (mapped == execution.Status)
        {
            return;
        }

        execution.UpdateStatus(mapped, mapped is PipelineStatus.Succeeded or PipelineStatus.Failed or PipelineStatus.Cancelled ? DateTimeOffset.UtcNow : null);

        var requestId = new DeploymentRequestId(execution.DeploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            _logger.LogWarning("Pipeline execution for deployment request {DeploymentRequestId} references a request that no longer exists.", execution.DeploymentRequestId);
            return;
        }

        if (request.Status == DeploymentRequestStatus.DeploymentQueued)
        {
            request.TransitionTo(DeploymentRequestStatus.Deploying, WorkerActor.Identity, request.Version);
            await _requests.SaveAsync(request, cancellationToken);
        }

        switch (mapped)
        {
            case PipelineStatus.Succeeded:
                request.TransitionTo(DeploymentRequestStatus.Deployed, WorkerActor.Identity, request.Version);
                await _requests.SaveAsync(request, cancellationToken);
                await _evidenceFinalizer.FinalizeAsync(request, execution, succeeded: true, cancellationToken);
                break;
            case PipelineStatus.Failed:
            case PipelineStatus.Cancelled:
                request.TransitionTo(DeploymentRequestStatus.DeploymentFailed, WorkerActor.Identity, request.Version);
                await _requests.SaveAsync(request, cancellationToken);
                await _evidenceFinalizer.FinalizeAsync(request, execution, succeeded: false, cancellationToken);
                break;
        }
    }

    private static PipelineStatus MapStatus(string statusName, string? result) => (statusName, result?.ToLowerInvariant()) switch
    {
        ("completed", "succeeded") => PipelineStatus.Succeeded,
        ("completed", "failed") => PipelineStatus.Failed,
        ("completed", "canceled") => PipelineStatus.Cancelled,
        ("cancelling", _) => PipelineStatus.Running,
        ("inprogress", _) => PipelineStatus.Running,
        ("notfound", _) => PipelineStatus.Failed,
        _ => PipelineStatus.Queued,
    };
}
