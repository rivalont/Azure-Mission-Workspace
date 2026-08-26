using System.Text.Json;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.Infrastructure.Evidence;

namespace AzureMissionWorkspace.Worker;

/// <summary>
/// Assembles and persists the final <see cref="DeploymentEvidence"/> package for a deployment
/// request once its Azure DevOps pipeline execution reaches a terminal state, then transitions the
/// request to <see cref="DeploymentRequestStatus.EvidenceFinalized"/>. All parameter values are
/// redacted before hashing -- secret values are never written into evidence artifacts.
/// </summary>
public sealed class DeploymentEvidenceFinalizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDeploymentEvidenceRepository _evidenceRepository;
    private readonly IDeploymentRequestRepository _requests;
    private readonly ILogger<DeploymentEvidenceFinalizer> _logger;

    public DeploymentEvidenceFinalizer(
        IDeploymentEvidenceRepository evidenceRepository,
        IDeploymentRequestRepository requests,
        ILogger<DeploymentEvidenceFinalizer> logger)
    {
        _evidenceRepository = evidenceRepository;
        _requests = requests;
        _logger = logger;
    }

    public async Task FinalizeAsync(DeploymentRequest request, PipelineExecution execution, bool succeeded, CancellationToken cancellationToken)
    {
        var builder = new EvidenceBuilder()
            .AddArtifact("deployment-request.json", JsonSerializer.Serialize(new
            {
                DeploymentRequestId = request.Id.Value,
                request.CorrelationId,
                RequestorObjectId = request.Requestor.ObjectId,
                EnvironmentProfileId = request.EnvironmentProfileId.Value,
                request.NaturalLanguageRequest,
                ServicePatternId = request.SelectedServicePatternId?.Value,
                ServicePatternVersion = request.SelectedServicePatternVersion?.Value,
                Status = request.Status.ToString(),
            }, SerializerOptions), storageUri: $"evidence://deployment-requests/{request.Id.Value}/deployment-request.json")
            .AddArtifact("rendered-parameters.bicepparam", JsonSerializer.Serialize(request.Parameters?.ToRedactedDictionary() ?? new Dictionary<string, string>(), SerializerOptions), $"evidence://deployment-requests/{request.Id.Value}/rendered-parameters.json")
            .AddArtifact("pipeline-execution.json", JsonSerializer.Serialize(new
            {
                execution.Id,
                execution.DeploymentRequestId,
                execution.PipelineName,
                execution.BuildId,
                Status = execution.Status.ToString(),
                execution.QueuedAtUtc,
                execution.CompletedAtUtc,
            }, SerializerOptions), $"evidence://deployment-requests/{request.Id.Value}/pipeline-execution.json")
            .AddArtifact("deployment-result.json", JsonSerializer.Serialize(new { succeeded, finalizedAtUtc = DateTimeOffset.UtcNow }, SerializerOptions), $"evidence://deployment-requests/{request.Id.Value}/deployment-result.json");

        var evidence = builder.Build(request.Id.Value);
        await _evidenceRepository.SaveAsync(evidence, cancellationToken);

        request.TransitionTo(DeploymentRequestStatus.EvidenceFinalized, WorkerActor.Identity, request.Version);
        await _requests.SaveAsync(request, cancellationToken);

        _logger.LogInformation(
            "Finalized deployment evidence for deployment request {DeploymentRequestId} (succeeded: {Succeeded}).",
            request.Id.Value,
            succeeded);
    }
}
