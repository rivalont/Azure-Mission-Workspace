using AzureMissionWorkspace.Application.Abstractions.AzureDevOps;
using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Application.Abstractions.Policy;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.Tests;

internal sealed class TestDeploymentRequestRepository : IDeploymentRequestRepository
{
    private readonly Dictionary<Guid, DeploymentRequest> _requests = [];

    public int AddCalls { get; private set; }

    public int SaveCalls { get; private set; }

    public Task<DeploymentRequest?> FindByIdAsync(DeploymentRequestId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_requests.TryGetValue(id.Value, out var request) ? request : null);

    public Task<IReadOnlyCollection<DeploymentRequest>> FindByRequestorAsync(string requestorObjectId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<DeploymentRequest>>(_requests.Values.Where(r => string.Equals(r.Requestor.ObjectId, requestorObjectId, StringComparison.OrdinalIgnoreCase)).ToArray());

    public Task AddAsync(DeploymentRequest request, CancellationToken cancellationToken = default)
    {
        AddCalls++;
        _requests[request.Id.Value] = request;
        return Task.CompletedTask;
    }

    public Task SaveAsync(DeploymentRequest request, CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        _requests[request.Id.Value] = request;
        return Task.CompletedTask;
    }
}

internal sealed class TestServicePatternRepository(params ServicePattern[] patterns) : IServicePatternRepository
{
    private readonly Dictionary<string, ServicePattern> _patterns = patterns.ToDictionary(static p => $"{p.Id.Value}@{p.Version.Value}", StringComparer.OrdinalIgnoreCase);

    public Task<ServicePattern?> FindAsync(ServicePatternId id, ServicePatternVersion version, CancellationToken cancellationToken = default)
        => Task.FromResult(_patterns.TryGetValue($"{id.Value}@{version.Value}", out var pattern) ? pattern : null);

    public Task<ServicePattern?> FindLatestAsync(ServicePatternId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_patterns.Values.Where(p => p.Id.Equals(id)).OrderByDescending(p => p.Version.Value, StringComparer.Ordinal).FirstOrDefault());

    public Task<IReadOnlyCollection<ServicePattern>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<ServicePattern>>(_patterns.Values.ToArray());

    public Task<string?> GetInputSchemaJsonAsync(ServicePatternId id, ServicePatternVersion version, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}

internal sealed class TestEnvironmentProfileRepository(params EnvironmentProfile[] profiles) : IEnvironmentProfileRepository
{
    private readonly Dictionary<string, EnvironmentProfile> _profiles = profiles.ToDictionary(static p => p.Id.Value, StringComparer.OrdinalIgnoreCase);

    public Task<EnvironmentProfile?> FindByIdAsync(EnvironmentProfileId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_profiles.TryGetValue(id.Value, out var profile) ? profile : null);

    public Task<IReadOnlyCollection<EnvironmentProfile>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<EnvironmentProfile>>(_profiles.Values.ToArray());
}

internal sealed class TestApprovalRepository : IApprovalRepository
{
    private readonly Dictionary<Guid, List<ApprovalRequirement>> _requirements = [];
    private readonly Dictionary<Guid, List<ApprovalDecision>> _decisions = [];

    public Task<IReadOnlyCollection<ApprovalRequirement>> GetRequirementsAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<ApprovalRequirement>>(_requirements.TryGetValue(deploymentRequestId.Value, out var list) ? list.ToArray() : []);

    public Task AddRequirementAsync(ApprovalRequirement requirement, CancellationToken cancellationToken = default)
    {
        if (!_requirements.TryGetValue(requirement.DeploymentRequestId, out var list))
        {
            list = [];
            _requirements[requirement.DeploymentRequestId] = list;
        }

        list.Add(requirement);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ApprovalDecision>> GetDecisionsAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<ApprovalDecision>>(_decisions.TryGetValue(deploymentRequestId.Value, out var list) ? list.ToArray() : []);

    public Task RecordDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        if (!_decisions.TryGetValue(decision.DeploymentRequestId, out var list))
        {
            list = [];
            _decisions[decision.DeploymentRequestId] = list;
        }

        list.Add(decision);
        return Task.CompletedTask;
    }
}

internal sealed class TestDeploymentEvidenceRepository(params DeploymentEvidence[] evidence) : IDeploymentEvidenceRepository
{
    private readonly Dictionary<Guid, DeploymentEvidence> _evidence = evidence.ToDictionary(static e => e.DeploymentRequestId);

    public Task<DeploymentEvidence?> FindByDeploymentRequestIdAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default)
        => Task.FromResult(_evidence.TryGetValue(deploymentRequestId.Value, out var result) ? result : null);

    public Task SaveAsync(DeploymentEvidence evidence, CancellationToken cancellationToken = default)
    {
        _evidence[evidence.DeploymentRequestId] = evidence;
        return Task.CompletedTask;
    }
}

internal sealed class TestBicepParameterRenderer(string renderedOutput) : IBicepParameterRenderer
{
    public Task<string> RenderAsync(ServicePattern pattern, DeploymentRequest request, EnvironmentProfile environmentProfile, CancellationToken cancellationToken = default)
        => Task.FromResult(renderedOutput);
}

internal sealed class TestBicepCompiler(BicepCompilationResult result) : IBicepCompiler
{
    public Task<BicepCompilationResult> CompileAsync(string bicepFilePath, CancellationToken cancellationToken = default)
        => Task.FromResult(result);
}

internal sealed class TestBicepLinter(BicepLintResult result) : IBicepLinter
{
    public Task<BicepLintResult> LintAsync(string bicepFilePath, CancellationToken cancellationToken = default)
        => Task.FromResult(result);
}

internal sealed class TestTemplateValidationService(TemplateValidationResult result) : ITemplateValidationService
{
    public Task<TemplateValidationResult> ValidateAsync(string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default)
        => Task.FromResult(result);
}

internal sealed class TestWhatIfService(RawWhatIfResult result) : IWhatIfService
{
    public Task<RawWhatIfResult> ExecuteWhatIfAsync(string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default)
        => Task.FromResult(result);
}

internal sealed class TestWhatIfResultNormalizer(DeploymentPlan plan) : IWhatIfResultNormalizer
{
    public DeploymentPlan Normalize(Guid deploymentRequestId, RawWhatIfResult rawResult, AzureMissionWorkspace.Domain.Enums.EnvironmentType environmentType)
        => plan;
}

internal sealed class TestPolicyEvaluator(PolicyEvaluation evaluation) : IPolicyEvaluator
{
    public DeploymentPlan? CapturedPlan { get; private set; }

    public Task<PolicyEvaluation> EvaluateAsync(DeploymentRequest request, ServicePattern pattern, EnvironmentProfile environmentProfile, DeploymentPlan? plan, CancellationToken cancellationToken = default)
    {
        CapturedPlan = plan;
        return Task.FromResult(evaluation);
    }
}

internal sealed class TestAzureDevOpsClient : IAzureDevOpsClient, IApprovalService, IPipelineService
{
    public bool ApprovalSatisfied { get; set; } = true;

    public int BuildIdToReturn { get; set; } = 1001;

    public IRepositoryService Repositories => throw new NotSupportedException();

    public IPullRequestService PullRequests => throw new NotSupportedException();

    public IPipelineService Pipelines => this;

    public IApprovalService Approvals => this;

    public IArtifactService Artifacts => throw new NotSupportedException();

    public Task<bool> IsApprovalSatisfiedAsync(string deploymentRequestId, CancellationToken cancellationToken = default)
        => Task.FromResult(ApprovalSatisfied);

    public Task<int> QueueValidationPipelineAsync(string deploymentRequestId, string correlationId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<int> QueueDeploymentPipelineAsync(string deploymentRequestId, string correlationId, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildIdToReturn);

    public Task<PipelineRunStatus> GetStatusAsync(int buildId, CancellationToken cancellationToken = default)
        => Task.FromResult(new PipelineRunStatus(buildId, "completed", "succeeded"));
}

internal sealed class TestInputSchemaValidator : IInputSchemaValidator
{
    public InputSchemaValidationResult Validate(string inputSchemaJson, IReadOnlyDictionary<string, string> parameterValues)
        => new(true, []);
}
