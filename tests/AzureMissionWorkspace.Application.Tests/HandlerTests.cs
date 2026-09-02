using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Application.UseCases.Approvals;
using AzureMissionWorkspace.Application.UseCases.DeploymentPlans;
using AzureMissionWorkspace.Application.UseCases.DeploymentRequests;
using AzureMissionWorkspace.Application.UseCases.Deployments;
using AzureMissionWorkspace.Application.UseCases.Policy;
using AzureMissionWorkspace.Application.Validation;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.Exceptions;
using FluentAssertions;

namespace AzureMissionWorkspace.Application.Tests;

public sealed class HandlerTests
{
    [Fact]
    public async Task CreateDeploymentRequestHandler_creates_and_advances_request()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile(id: "azure-commercial-development");
        var requests = new TestDeploymentRequestRepository();
        var handler = new CreateDeploymentRequestHandler(
            requests,
            new TestEnvironmentProfileRepository(environmentProfile),
            new CreateDeploymentRequestInputValidator());

        var request = await handler.HandleAsync(new CreateDeploymentRequestInput(
            "requestor-1",
            "Requestor One",
            "requestor@example.com",
            environmentProfile.Id.Value,
            "Deploy an internal API"));

        request.Status.Should().Be(DeploymentRequestStatus.RequirementsComplete);
        request.Requestor.Roles.Should().ContainSingle().Which.Should().Be("DeploymentRequestor");
        request.EnvironmentProfileId.Should().Be(environmentProfile.Id);
        request.Version.Should().Be(2);
        requests.AddCalls.Should().Be(1);
        requests.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task SelectServicePatternHandler_selects_allowed_pattern()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var pattern = TestData.CreateServicePattern();
        var request = TestData.CreateRequest(environmentProfile);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);

        var handler = new SelectServicePatternHandler(
            requests,
            new TestServicePatternRepository(pattern),
            new TestEnvironmentProfileRepository(environmentProfile),
            new SelectServicePatternInputValidator());

        var updated = await handler.HandleAsync(new SelectServicePatternInput(
            request.Id.Value,
            pattern.Id.Value,
            pattern.Version.Value,
            request.Version));

        updated.Status.Should().Be(DeploymentRequestStatus.PatternSelected);
        updated.SelectedServicePatternId.Should().Be(pattern.Id);
        updated.SelectedServicePatternVersion.Should().Be(pattern.Version);
    }

    [Fact]
    public async Task UpdateDeploymentRequestHandler_renders_parameters_when_required_inputs_are_present()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var pattern = TestData.CreateServicePattern();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.PatternSelected, pattern);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);

        var handler = new UpdateDeploymentRequestHandler(requests, new TestServicePatternRepository(pattern), new TestInputSchemaValidator());

        var updated = await handler.HandleAsync(new UpdateDeploymentRequestInput(
            request.Id.Value,
            TestData.CreateValidParameterValues(),
            request.Version));

        updated.Status.Should().Be(DeploymentRequestStatus.ParametersRendered);
        updated.Parameters.Should().NotBeNull();
        updated.Parameters!.ToRedactedDictionary()["apiSecret"].Should().Be("***redacted***");
    }

    [Fact]
    public async Task UpdateDeploymentRequestHandler_throws_when_required_input_is_missing()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var pattern = TestData.CreateServicePattern();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.PatternSelected, pattern);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var parameterValues = TestData.CreateValidParameterValues();
        parameterValues.Remove("containerImage");

        var handler = new UpdateDeploymentRequestHandler(requests, new TestServicePatternRepository(pattern), new TestInputSchemaValidator());

        var act = () => handler.HandleAsync(new UpdateDeploymentRequestInput(request.Id.Value, parameterValues, request.Version));

        await act.Should().ThrowAsync<InvalidDeploymentParametersException>()
            .WithMessage("*containerImage*");
    }

    [Fact]
    public async Task CancelDeploymentRequestHandler_cancels_request()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var request = TestData.CreateRequest(environmentProfile);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var handler = new CancelDeploymentRequestHandler(requests);

        var cancelled = await handler.HandleAsync(request.Id.Value, request.Version);

        cancelled.Status.Should().Be(DeploymentRequestStatus.Cancelled);
    }

    [Fact]
    public async Task ValidateDeploymentRequestHandler_validates_and_advances_request()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var pattern = TestData.CreateServicePattern();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.ParametersRendered, pattern);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);

        var handler = new ValidateDeploymentRequestHandler(
            requests,
            new TestServicePatternRepository(pattern),
            new TestEnvironmentProfileRepository(environmentProfile),
            new TestBicepParameterRenderer("{\"parameters\":{}}"),
            new TestBicepCompiler(new BicepCompilationResult(true, "{\"resources\":[]}", [])),
            new TestBicepLinter(new BicepLintResult(true, [])),
            new TestTemplateValidationService(new TemplateValidationResult(true, [])));

        var outcome = await handler.HandleAsync(request.Id.Value, "main.bicep");

        outcome.Succeeded.Should().BeTrue();
        outcome.RenderedParametersJson.Should().Be("{\"parameters\":{}}");
        outcome.CompiledTemplateJson.Should().Be("{\"resources\":[]}");
        request.Status.Should().Be(DeploymentRequestStatus.ValidationPassed);
        requests.SaveCalls.Should().Be(2);
    }

    [Fact]
    public async Task GenerateDeploymentPlanHandler_normalizes_plan_and_advances_request()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.ValidationPassed);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var plan = TestData.CreatePlan(request.Id.Value, DeploymentRisk.Low, PlanChangeType.Create);

        var handler = new GenerateDeploymentPlanHandler(
            requests,
            new TestEnvironmentProfileRepository(environmentProfile),
            new TestWhatIfService(new RawWhatIfResult("{\"changes\":[]}")),
            new TestWhatIfResultNormalizer(plan));

        var result = await handler.HandleAsync(request.Id.Value, "{}", "{}");

        result.Should().BeSameAs(plan);
        request.Status.Should().Be(DeploymentRequestStatus.PlanGenerated);
    }

    [Fact]
    public async Task EvaluatePolicyComplianceHandler_returns_policy_evaluation()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var pattern = TestData.CreateServicePattern();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.PatternSelected, pattern);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var plan = TestData.CreatePlan(request.Id.Value);
        var evaluation = new PolicyEvaluation(Guid.NewGuid(), request.Id.Value, [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var evaluator = new TestPolicyEvaluator(evaluation);

        var handler = new EvaluatePolicyComplianceHandler(
            requests,
            new TestServicePatternRepository(pattern),
            new TestEnvironmentProfileRepository(environmentProfile),
            evaluator);

        var result = await handler.HandleAsync(request.Id.Value, plan);

        result.Should().BeSameAs(evaluation);
        evaluator.CapturedPlan.Should().BeSameAs(plan);
    }

    [Fact]
    public async Task SubmitDeploymentForApprovalHandler_persists_requirements_and_advances_request()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Development);
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.PlanGenerated);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var approvals = new TestApprovalRepository();
        var plan = TestData.CreatePlan(request.Id.Value, DeploymentRisk.Low, PlanChangeType.Create);

        var handler = new SubmitDeploymentForApprovalHandler(
            requests,
            new TestEnvironmentProfileRepository(environmentProfile),
            approvals);

        var requirements = await handler.HandleAsync(request.Id.Value, plan);

        requirements.Should().ContainSingle();
        requirements.Single().RequiredApproverCount.Should().Be(1);
        requirements.Single().RequiresDistinctFromRequestor.Should().BeFalse();
        request.Status.Should().Be(DeploymentRequestStatus.AwaitingApproval);
    }

    [Fact]
    public async Task RecordApprovalDecisionHandler_approves_request_when_all_requirements_are_met()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.AwaitingApproval);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var approvals = new TestApprovalRepository();
        var requirement = new ApprovalRequirement(Guid.NewGuid(), request.Id.Value, "DeploymentApprover", 1, requiresDistinctFromRequestor: false);
        await approvals.AddRequirementAsync(requirement);

        var handler = new RecordApprovalDecisionHandler(requests, approvals);

        var updated = await handler.HandleAsync(new RecordApprovalDecisionInput(
            request.Id.Value,
            requirement.Id,
            "approver-1",
            ApprovalStatus.Approved,
            "Looks good"));

        updated.Status.Should().Be(DeploymentRequestStatus.Approved);
    }

    [Fact]
    public async Task RecordApprovalDecisionHandler_enforces_separation_of_duties()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile(environmentType: EnvironmentType.Production);
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.AwaitingApproval, requestorObjectId: "requestor-1");
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var approvals = new TestApprovalRepository();
        var requirement = new ApprovalRequirement(Guid.NewGuid(), request.Id.Value, "DeploymentApprover", 1, requiresDistinctFromRequestor: true);
        await approvals.AddRequirementAsync(requirement);
        var handler = new RecordApprovalDecisionHandler(requests, approvals);

        var act = () => handler.HandleAsync(new RecordApprovalDecisionInput(
            request.Id.Value,
            requirement.Id,
            request.Requestor.ObjectId,
            ApprovalStatus.Approved,
            null));

        await act.Should().ThrowAsync<SeparationOfDutiesViolationException>();
    }

    [Fact]
    public async Task QueueDeploymentHandler_queues_pipeline_and_updates_status()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var request = TestData.CreateRequest(environmentProfile, DeploymentRequestStatus.Approved);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);
        var azureDevOps = new TestAzureDevOpsClient { BuildIdToReturn = 4242, ApprovalSatisfied = true };
        var handler = new QueueDeploymentHandler(requests, azureDevOps);

        var execution = await handler.HandleAsync(request.Id.Value);

        execution.BuildId.Should().Be(4242);
        execution.Status.Should().Be(PipelineStatus.Queued);
        request.Status.Should().Be(DeploymentRequestStatus.DeploymentQueued);
    }

    [Fact]
    public async Task GetDeploymentStatusHandler_returns_request()
    {
        var environmentProfile = TestData.CreateEnvironmentProfile();
        var request = TestData.CreateRequest(environmentProfile);
        var requests = new TestDeploymentRequestRepository();
        await requests.AddAsync(request);

        var result = await new GetDeploymentStatusHandler(requests).HandleAsync(request.Id.Value);

        result.Should().BeSameAs(request);
    }

    [Fact]
    public async Task GetDeploymentEvidenceHandler_returns_saved_evidence()
    {
        var evidence = TestData.CreateEvidence(Guid.NewGuid());
        var repository = new TestDeploymentEvidenceRepository(evidence);

        var result = await new GetDeploymentEvidenceHandler(repository).HandleAsync(evidence.DeploymentRequestId);

        result.Should().BeSameAs(evidence);
    }
}
