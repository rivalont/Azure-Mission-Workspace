using System.ComponentModel;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Application.UseCases.DeploymentPlans;
using AzureMissionWorkspace.Application.UseCases.DeploymentRequests;
using AzureMissionWorkspace.Application.UseCases.Deployments;
using AzureMissionWorkspace.McpServer.Dtos;
using AzureMissionWorkspace.McpServer.Security;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// MCP tools that create and mutate deployment requests. Every mutating tool preserves the
/// authenticated human actor (never the Azure DevOps service connection or a managed identity) in
/// the resulting audit trail via <see cref="ICurrentActorProvider"/>.
/// </summary>
[McpServerToolType]
public sealed class DeploymentRequestTools
{
    private readonly CreateDeploymentRequestHandler _create;
    private readonly SelectServicePatternHandler _selectPattern;
    private readonly UpdateDeploymentRequestHandler _update;
    private readonly CancelDeploymentRequestHandler _cancel;
    private readonly ValidateDeploymentRequestHandler _validate;
    private readonly GetDeploymentStatusHandler _getStatus;
    private readonly ICurrentActorProvider _currentActor;

    public DeploymentRequestTools(
        CreateDeploymentRequestHandler create,
        SelectServicePatternHandler selectPattern,
        UpdateDeploymentRequestHandler update,
        CancelDeploymentRequestHandler cancel,
        ValidateDeploymentRequestHandler validate,
        GetDeploymentStatusHandler getStatus,
        ICurrentActorProvider currentActor)
    {
        _create = create;
        _selectPattern = selectPattern;
        _update = update;
        _cancel = cancel;
        _validate = validate;
        _getStatus = getStatus;
        _currentActor = currentActor;
    }

    [McpServerTool(Name = "create_deployment_request", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Mutating. Creates a new deployment-request draft from a natural-language description and an environment profile id. Does not select a pattern or trigger any deployment.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentRequestDto> CreateDeploymentRequestAsync(
        [Description("A natural-language description of the desired Azure capability.")] string naturalLanguageRequest,
        [Description("The id of the platform-administrator-controlled environment profile to target.")] string environmentProfileId,
        CancellationToken cancellationToken)
    {
        var actor = _currentActor.GetCurrentActor();
        var input = new CreateDeploymentRequestInput(actor.ObjectId, actor.DisplayName, actor.UserPrincipalName, environmentProfileId, naturalLanguageRequest);
        var request = await _create.HandleAsync(input, cancellationToken);
        return DeploymentRequestDto.FromDomain(request);
    }

    [McpServerTool(Name = "get_deployment_request", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Retrieves the current state of a deployment request by id.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentRequestDto> GetDeploymentRequestAsync(
        [Description("The deployment request id (GUID).")] Guid deploymentRequestId,
        CancellationToken cancellationToken)
    {
        var request = await _getStatus.HandleAsync(deploymentRequestId, cancellationToken);
        return DeploymentRequestDto.FromDomain(request);
    }

    [McpServerTool(Name = "update_deployment_request", ReadOnly = false, Destructive = false, Idempotent = true)]
    [Description("Mutating. Supplies candidate parameter values for the deployment request's selected service pattern. Values are validated against the pattern's required inputs before being rendered.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentRequestDto> UpdateDeploymentRequestAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("Parameter name/value pairs required or optional for the selected service pattern. Never include literal secret values -- use Key Vault references instead.")] IReadOnlyDictionary<string, string> parameterValues,
        [Description("The expected current version of the deployment request (optimistic concurrency).")] int expectedVersion,
        CancellationToken cancellationToken)
    {
        var input = new UpdateDeploymentRequestInput(deploymentRequestId, parameterValues, expectedVersion);
        var request = await _update.HandleAsync(input, cancellationToken);
        return DeploymentRequestDto.FromDomain(request);
    }

    [McpServerTool(Name = "select_service_pattern", ReadOnly = false, Destructive = false, Idempotent = true)]
    [Description("Mutating. Attaches an explicitly selected, approved service pattern to a deployment request. Never invoked automatically from a recommendation -- the requestor must confirm the selection.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentRequestDto> SelectServicePatternAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("The service pattern id to select.")] string servicePatternId,
        [Description("The service pattern version to select.")] string servicePatternVersion,
        [Description("The expected current version of the deployment request (optimistic concurrency).")] int expectedVersion,
        CancellationToken cancellationToken)
    {
        var input = new SelectServicePatternInput(deploymentRequestId, servicePatternId, servicePatternVersion, expectedVersion);
        var request = await _selectPattern.HandleAsync(input, cancellationToken);
        return DeploymentRequestDto.FromDomain(request);
    }

    [McpServerTool(Name = "validate_deployment_request", ReadOnly = false, Destructive = false, Idempotent = false)]
    [Description("Mutating (validation only, never deploys). Renders deterministic parameters and runs the approved Bicep module through format/build/lint/template-validation checks.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<ValidationOutcomeDto> ValidateDeploymentRequestAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("Path to the service pattern's approved Bicep entry-point file.")] string bicepEntryPointPath,
        CancellationToken cancellationToken)
    {
        var outcome = await _validate.HandleAsync(deploymentRequestId, bicepEntryPointPath, cancellationToken);
        return new ValidationOutcomeDto(outcome.Succeeded, outcome.Diagnostics, outcome.RenderedParametersJson, outcome.CompiledTemplateJson);
    }

    [McpServerTool(Name = "cancel_deployment_request", ReadOnly = false, Destructive = true, Idempotent = true)]
    [Description("Mutating. Cancels a deployment request that has not yet reached a terminal status. Does not affect any resources already deployed.")]
    [Authorize(Policy = AuthorizationPolicyNames.DeploymentRequestor)]
    public async Task<DeploymentRequestDto> CancelDeploymentRequestAsync(
        [Description("The deployment request id.")] Guid deploymentRequestId,
        [Description("The expected current version of the deployment request (optimistic concurrency).")] int expectedVersion,
        CancellationToken cancellationToken)
    {
        var request = await _cancel.HandleAsync(deploymentRequestId, expectedVersion, cancellationToken);
        return DeploymentRequestDto.FromDomain(request);
    }
}

/// <summary>Result of the validate_deployment_request tool. Diagnostics and the rendered artifact never include secret parameter values.</summary>
public sealed record ValidationOutcomeDto(bool Succeeded, IReadOnlyCollection<string> Diagnostics, string RenderedParametersJson, string? CompiledTemplateJson);
