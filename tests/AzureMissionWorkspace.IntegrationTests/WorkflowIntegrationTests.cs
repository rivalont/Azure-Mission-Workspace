using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Application.Abstractions.Policy;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Application.UseCases.Approvals;
using AzureMissionWorkspace.Application.UseCases.DeploymentPlans;
using AzureMissionWorkspace.Application.UseCases.DeploymentRequests;
using AzureMissionWorkspace.Application.UseCases.Deployments;
using AzureMissionWorkspace.Application.UseCases.Policy;
using AzureMissionWorkspace.Application.Validation;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Infrastructure.Bicep;
using AzureMissionWorkspace.Infrastructure.Evidence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMissionWorkspace.IntegrationTests;

public sealed class WorkflowIntegrationTests
{
    [Fact]
    public async Task Full_workflow_runs_end_to_end_with_fake_infrastructure()
    {
        using var serviceProvider = TestHost.BuildServiceProvider();
        var requests = serviceProvider.GetRequiredService<IDeploymentRequestRepository>();
        var patterns = serviceProvider.GetRequiredService<IServicePatternRepository>();
        var environments = serviceProvider.GetRequiredService<IEnvironmentProfileRepository>();
        var approvals = serviceProvider.GetRequiredService<IApprovalRepository>();
        var evidenceRepository = serviceProvider.GetRequiredService<IDeploymentEvidenceRepository>();
        var renderer = serviceProvider.GetRequiredService<IBicepParameterRenderer>();
        var compiler = serviceProvider.GetRequiredService<IBicepCompiler>();
        var linter = serviceProvider.GetRequiredService<IBicepLinter>();
        var templateValidation = serviceProvider.GetRequiredService<ITemplateValidationService>();
        var whatIf = serviceProvider.GetRequiredService<IWhatIfService>();
        var normalizer = serviceProvider.GetRequiredService<IWhatIfResultNormalizer>();
        var policyEvaluator = serviceProvider.GetRequiredService<IPolicyEvaluator>();

        var createHandler = new CreateDeploymentRequestHandler(requests, environments, new CreateDeploymentRequestInputValidator());
        var request = await createHandler.HandleAsync(new CreateDeploymentRequestInput(
            "requestor-1",
            "Requestor One",
            "requestor@example.com",
            "azure-commercial-development",
            "Deploy an internal web API"));

        var selectHandler = new SelectServicePatternHandler(requests, patterns, environments, new SelectServicePatternInputValidator());
        request = await selectHandler.HandleAsync(new SelectServicePatternInput(request.Id.Value, "internal-web-api", "1.0.0", request.Version));

        var updateHandler = new UpdateDeploymentRequestHandler(requests, patterns);
        request = await updateHandler.HandleAsync(new UpdateDeploymentRequestInput(
            request.Id.Value,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["workloadName"] = "amw-api",
                ["location"] = "eastus",
                ["environment"] = "dev",
                ["costCenter"] = "CC100",
                ["owner"] = "owner@example.com",
                ["dataClassification"] = "Internal",
                ["logAnalyticsWorkspaceResourceId"] = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-observability/providers/Microsoft.OperationalInsights/workspaces/law-platform-dev",
                ["subnetResourceId"] = "/subscriptions/00000000-0000-0000-0000-000000000000/subnets/snet-appsvc-dev",
                ["containerImage"] = "example.azurecr.io/amw-api:1.0.0",
                ["skuName"] = "P1v3",
            },
            request.Version));

        var validateHandler = new ValidateDeploymentRequestHandler(requests, patterns, environments, renderer, compiler, linter, templateValidation);
        var bicepPath = Path.Combine(TestHost.FindRepositoryRoot(), "service-patterns", "internal-web-api", "main.bicep");
        var validation = await validateHandler.HandleAsync(request.Id.Value, bicepPath);

        var planHandler = new GenerateDeploymentPlanHandler(requests, environments, whatIf, normalizer);
        var plan = await planHandler.HandleAsync(request.Id.Value, validation.CompiledTemplateJson!, validation.RenderedParametersJson);

        var policyHandler = new EvaluatePolicyComplianceHandler(requests, patterns, environments, policyEvaluator);
        var policy = await policyHandler.HandleAsync(request.Id.Value, plan);

        var approvalHandler = new SubmitDeploymentForApprovalHandler(requests, environments, approvals);
        var requirements = await approvalHandler.HandleAsync(request.Id.Value, plan);

        var decisionHandler = new RecordApprovalDecisionHandler(requests, approvals);
        request = await decisionHandler.HandleAsync(new RecordApprovalDecisionInput(
            request.Id.Value,
            requirements.Single().Id,
            "approver-1",
            ApprovalStatus.Approved,
            "Approved"));

        var queueHandler = new QueueDeploymentHandler(requests, serviceProvider.GetRequiredService<AzureMissionWorkspace.Application.Abstractions.AzureDevOps.IAzureDevOpsClient>());
        var execution = await queueHandler.HandleAsync(request.Id.Value);

        var evidence = new EvidenceBuilder()
            .AddArtifact("what-if-normalized.json", "{}", "memory://what-if-normalized.json")
            .AddArtifact("policy-evaluation.json", "{}", "memory://policy-evaluation.json")
            .Build(request.Id.Value);
        await evidenceRepository.SaveAsync(evidence);

        var status = await new GetDeploymentStatusHandler(requests).HandleAsync(request.Id.Value);
        var loadedEvidence = await new GetDeploymentEvidenceHandler(evidenceRepository).HandleAsync(request.Id.Value);

        validation.Succeeded.Should().BeTrue();
        plan.Changes.Should().ContainSingle();
        plan.Changes.Single().ChangeType.Should().Be(PlanChangeType.Create);
        policy.HasBlockingFindings.Should().BeFalse();
        requirements.Should().ContainSingle();
        execution.BuildId.Should().NotBeNull();
        status.Status.Should().Be(DeploymentRequestStatus.DeploymentQueued);
        loadedEvidence.Should().NotBeNull();
        loadedEvidence!.Artifacts.Keys.Should().BeEquivalentTo(["what-if-normalized.json", "policy-evaluation.json"]);
    }
}
