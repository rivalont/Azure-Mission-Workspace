using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Exceptions;
using AzureMissionWorkspace.Domain.ValueObjects;
using FluentValidation;

namespace AzureMissionWorkspace.Application.UseCases.DeploymentRequests;

/// <summary>Use case: attach a chosen, approved service pattern to a deployment request.</summary>
public sealed class SelectServicePatternHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IServicePatternRepository _patterns;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IValidator<SelectServicePatternInput> _validator;

    public SelectServicePatternHandler(
        IDeploymentRequestRepository requests,
        IServicePatternRepository patterns,
        IEnvironmentProfileRepository environmentProfiles,
        IValidator<SelectServicePatternInput> validator)
    {
        _requests = requests;
        _patterns = patterns;
        _environmentProfiles = environmentProfiles;
        _validator = validator;
    }

    public async Task<DeploymentRequest> HandleAsync(SelectServicePatternInput input, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(input, cancellationToken);

        var requestId = new DeploymentRequestId(input.DeploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{input.DeploymentRequestId}' was not found.");

        var patternId = new ServicePatternId(input.ServicePatternId);
        var patternVersion = new ServicePatternVersion(input.ServicePatternVersion);
        var pattern = await _patterns.FindAsync(patternId, patternVersion, cancellationToken)
            ?? throw new ValidationException($"Service pattern '{input.ServicePatternId}@{input.ServicePatternVersion}' was not found in the approved catalog.");

        if (pattern.IsDeprecated)
        {
            throw new ValidationException($"Service pattern '{input.ServicePatternId}@{input.ServicePatternVersion}' is deprecated and cannot be selected for new deployment requests.");
        }

        var profile = await _environmentProfiles.FindByIdAsync(request.EnvironmentProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"Environment profile '{request.EnvironmentProfileId}' referenced by the deployment request no longer exists.");

        if (!profile.AllowsServicePattern(patternId))
        {
            throw new ValidationException($"Environment profile '{profile.Id}' does not allow service pattern '{patternId}'.");
        }

        if (!pattern.SupportsCloud(profile.Cloud))
        {
            throw new ValidationException($"Service pattern '{patternId}' does not support cloud '{profile.Cloud}'.");
        }

        if (!pattern.SupportsEnvironmentType(profile.EnvironmentType))
        {
            throw new ValidationException($"Service pattern '{patternId}' does not support environment type '{profile.EnvironmentType}'.");
        }

        request.SelectServicePattern(patternId, patternVersion, request.Requestor, input.ExpectedVersion);
        await _requests.SaveAsync(request, cancellationToken);
        return request;
    }
}

/// <summary>Use case: collect and validate requestor-supplied parameter values, then render them into the request.</summary>
public sealed class UpdateDeploymentRequestHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IServicePatternRepository _patterns;

    public UpdateDeploymentRequestHandler(IDeploymentRequestRepository requests, IServicePatternRepository patterns)
    {
        _requests = requests;
        _patterns = patterns;
    }

    public async Task<DeploymentRequest> HandleAsync(UpdateDeploymentRequestInput input, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(input.DeploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{input.DeploymentRequestId}' was not found.");

        if (request.SelectedServicePatternId is null || request.SelectedServicePatternVersion is null)
        {
            throw new InvalidOperationException("A service pattern must be selected before parameters can be supplied.");
        }

        var pattern = await _patterns.FindAsync(request.SelectedServicePatternId.Value, request.SelectedServicePatternVersion.Value, cancellationToken)
            ?? throw new InvalidOperationException("The selected service pattern could not be re-resolved.");

        var missingRequired = pattern.RequiredInputs
            .Where(required => !input.ParameterValues.ContainsKey(required.Name))
            .Select(required => required.Name)
            .ToArray();

        if (missingRequired.Length > 0)
        {
            throw new InvalidDeploymentParametersException($"Missing required input(s): {string.Join(", ", missingRequired)}.");
        }

        var parameters = new DeploymentParameters(pattern.SecretInputs);
        foreach (var (name, value) in input.ParameterValues)
        {
            parameters.Set(name, value);
        }

        request.RenderParameters(parameters, request.Requestor, input.ExpectedVersion);
        await _requests.SaveAsync(request, cancellationToken);
        return request;
    }
}

/// <summary>Use case: cancel a deployment request that has not yet reached a terminal status.</summary>
public sealed class CancelDeploymentRequestHandler
{
    private readonly IDeploymentRequestRepository _requests;

    public CancelDeploymentRequestHandler(IDeploymentRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<DeploymentRequest> HandleAsync(Guid deploymentRequestId, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");

        request.TransitionTo(Domain.Enums.DeploymentRequestStatus.Cancelled, request.Requestor, expectedVersion);
        await _requests.SaveAsync(request, cancellationToken);
        return request;
    }
}
