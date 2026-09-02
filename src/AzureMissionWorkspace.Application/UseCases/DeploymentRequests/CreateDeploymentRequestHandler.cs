using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;
using FluentValidation;

namespace AzureMissionWorkspace.Application.UseCases.DeploymentRequests;

/// <summary>Use case: create a new deployment-request draft from a natural-language description.</summary>
public sealed class CreateDeploymentRequestHandler
{
    private readonly IDeploymentRequestRepository _repository;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IValidator<CreateDeploymentRequestInput> _validator;

    public CreateDeploymentRequestHandler(
        IDeploymentRequestRepository repository,
        IEnvironmentProfileRepository environmentProfiles,
        IValidator<CreateDeploymentRequestInput> validator)
    {
        _repository = repository;
        _environmentProfiles = environmentProfiles;
        _validator = validator;
    }

    public async Task<DeploymentRequest> HandleAsync(CreateDeploymentRequestInput input, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(input, cancellationToken);

        var environmentProfileId = new EnvironmentProfileId(input.EnvironmentProfileId);
        var profile = await _environmentProfiles.FindByIdAsync(environmentProfileId, cancellationToken)
            ?? throw new ValidationException($"Environment profile '{input.EnvironmentProfileId}' was not found.");

        var actor = new ActorIdentity(input.RequestorObjectId, input.RequestorDisplayName, input.RequestorUpn, ["DeploymentRequestor"]);

        var request = new DeploymentRequest(
            DeploymentRequestId.New(),
            CorrelationId.New(),
            actor,
            profile.Id,
            input.NaturalLanguageRequest);

        await _repository.AddAsync(request, cancellationToken);

        // The natural-language requirement description is captured at creation time; the
        // request immediately advances to RequirementsComplete. Structured inputs required by a
        // specific service pattern are collected later, once a pattern is selected.
        request.TransitionTo(Domain.Enums.DeploymentRequestStatus.RequirementsComplete, actor, expectedVersion: request.Version);
        await _repository.SaveAsync(request, cancellationToken);

        return request;
    }
}
