using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.Exceptions;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.UseCases.DeploymentPlans;

/// <summary>
/// Use case: compile, lint, and validate the approved Bicep module for a deployment request's
/// selected service pattern, using deterministically rendered parameters. This is the "validation
/// pipeline" step in the conversational workflow; it never accepts or generates arbitrary Bicep.
/// </summary>
public sealed class ValidateDeploymentRequestHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IServicePatternRepository _patterns;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IBicepParameterRenderer _parameterRenderer;
    private readonly IBicepCompiler _compiler;
    private readonly IBicepLinter _linter;
    private readonly ITemplateValidationService _templateValidation;

    public ValidateDeploymentRequestHandler(
        IDeploymentRequestRepository requests,
        IServicePatternRepository patterns,
        IEnvironmentProfileRepository environmentProfiles,
        IBicepParameterRenderer parameterRenderer,
        IBicepCompiler compiler,
        IBicepLinter linter,
        ITemplateValidationService templateValidation)
    {
        _requests = requests;
        _patterns = patterns;
        _environmentProfiles = environmentProfiles;
        _parameterRenderer = parameterRenderer;
        _compiler = compiler;
        _linter = linter;
        _templateValidation = templateValidation;
    }

    public async Task<ValidationOutcome> HandleAsync(Guid deploymentRequestId, string bicepEntryPointPath, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");

        if (request.SelectedServicePatternId is null || request.Parameters is null)
        {
            throw new InvalidOperationException("A service pattern and rendered parameters are required before validation.");
        }

        var pattern = await _patterns.FindAsync(request.SelectedServicePatternId.Value, request.SelectedServicePatternVersion!.Value, cancellationToken)
            ?? throw new InvalidOperationException("The selected service pattern could not be re-resolved.");

        var profile = await _environmentProfiles.FindByIdAsync(request.EnvironmentProfileId, cancellationToken)
            ?? throw new InvalidOperationException("The referenced environment profile no longer exists.");

        request.TransitionTo(DeploymentRequestStatus.ValidationInProgress, request.Requestor, request.Version);
        await _requests.SaveAsync(request, cancellationToken);

        var renderedParametersJson = await _parameterRenderer.RenderAsync(pattern, request, profile, cancellationToken);
        var lintResult = await _linter.LintAsync(bicepEntryPointPath, cancellationToken);
        var compilationResult = await _compiler.CompileAsync(bicepEntryPointPath, cancellationToken);

        var passed = lintResult.Succeeded && compilationResult.Succeeded;
        TemplateValidationResult? templateValidationResult = null;

        if (passed && compilationResult.CompiledTemplateJson is not null)
        {
            templateValidationResult = await _templateValidation.ValidateAsync(compilationResult.CompiledTemplateJson, renderedParametersJson, cancellationToken);
            passed = templateValidationResult.IsValid;
        }

        request.TransitionTo(passed ? DeploymentRequestStatus.ValidationPassed : DeploymentRequestStatus.ValidationFailed, request.Requestor, request.Version);
        await _requests.SaveAsync(request, cancellationToken);

        var diagnostics = lintResult.Diagnostics
            .Concat(compilationResult.Diagnostics)
            .Concat(templateValidationResult?.Errors ?? [])
            .ToArray();

        return new ValidationOutcome(passed, renderedParametersJson, compilationResult.CompiledTemplateJson, diagnostics);
    }
}

/// <summary>Result of running the format/build/lint/template-validation pipeline for a deployment request.</summary>
public sealed record ValidationOutcome(bool Succeeded, string RenderedParametersJson, string? CompiledTemplateJson, IReadOnlyCollection<string> Diagnostics);

/// <summary>
/// Use case: execute ARM what-if against the validated template and normalize the result into a
/// <see cref="DeploymentPlan"/>, independent of Azure SDK response shapes.
/// </summary>
public sealed class GenerateDeploymentPlanHandler
{
    private readonly IDeploymentRequestRepository _requests;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IWhatIfService _whatIfService;
    private readonly IWhatIfResultNormalizer _normalizer;

    public GenerateDeploymentPlanHandler(
        IDeploymentRequestRepository requests,
        IEnvironmentProfileRepository environmentProfiles,
        IWhatIfService whatIfService,
        IWhatIfResultNormalizer normalizer)
    {
        _requests = requests;
        _environmentProfiles = environmentProfiles;
        _whatIfService = whatIfService;
        _normalizer = normalizer;
    }

    public async Task<DeploymentPlan> HandleAsync(Guid deploymentRequestId, string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default)
    {
        var requestId = new DeploymentRequestId(deploymentRequestId);
        var request = await _requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deployment request '{deploymentRequestId}' was not found.");

        if (request.Status != DeploymentRequestStatus.ValidationPassed)
        {
            throw new IllegalDeploymentRequestTransitionException(request.Status, DeploymentRequestStatus.PlanGenerated);
        }

        var profile = await _environmentProfiles.FindByIdAsync(request.EnvironmentProfileId, cancellationToken)
            ?? throw new InvalidOperationException("The referenced environment profile no longer exists.");

        var rawResult = await _whatIfService.ExecuteWhatIfAsync(compiledTemplateJson, renderedParametersJson, cancellationToken);
        var plan = _normalizer.Normalize(deploymentRequestId, rawResult, profile.EnvironmentType);

        request.TransitionTo(DeploymentRequestStatus.PlanGenerated, request.Requestor, request.Version);
        await _requests.SaveAsync(request, cancellationToken);

        return plan;
    }
}
