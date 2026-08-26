using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.Exceptions;
using AzureMissionWorkspace.Domain.ValueObjects;
using FluentAssertions;

namespace AzureMissionWorkspace.Domain.Tests;

public class DeploymentRequestStateMachineTests
{
    private static ActorIdentity Actor(string objectId = "actor-1") => new(objectId, "Test User", "test.user@example.com", ["DeploymentRequestor"]);

    private static DeploymentRequest CreateDraftRequest()
    {
        return new DeploymentRequest(
            DeploymentRequestId.New(),
            CorrelationId.New(),
            Actor(),
            new EnvironmentProfileId("azure-commercial-development"),
            "I need a small internal web API for a proof of concept.");
    }

    [Fact]
    public void Draft_to_RequirementsComplete_is_legal()
    {
        var request = CreateDraftRequest();

        request.TransitionTo(DeploymentRequestStatus.RequirementsComplete, Actor(), expectedVersion: 1);

        request.Status.Should().Be(DeploymentRequestStatus.RequirementsComplete);
        request.Version.Should().Be(2);
    }

    [Theory]
    [InlineData(DeploymentRequestStatus.PatternSelected)]
    [InlineData(DeploymentRequestStatus.ValidationPassed)]
    [InlineData(DeploymentRequestStatus.Approved)]
    [InlineData(DeploymentRequestStatus.Deployed)]
    [InlineData(DeploymentRequestStatus.EvidenceFinalized)]
    public void Draft_cannot_skip_directly_to_later_statuses(DeploymentRequestStatus illegalTarget)
    {
        var request = CreateDraftRequest();

        var act = () => request.TransitionTo(illegalTarget, Actor(), expectedVersion: 1);

        act.Should().Throw<IllegalDeploymentRequestTransitionException>()
            .Which.To.Should().Be(illegalTarget);
    }

    [Fact]
    public void Draft_can_be_cancelled()
    {
        var request = CreateDraftRequest();

        request.TransitionTo(DeploymentRequestStatus.Cancelled, Actor(), expectedVersion: 1);

        request.Status.Should().Be(DeploymentRequestStatus.Cancelled);
    }

    [Fact]
    public void Terminal_statuses_have_no_legal_next_state()
    {
        DeploymentRequestStateMachine.IsTerminal(DeploymentRequestStatus.EvidenceFinalized).Should().BeTrue();
        DeploymentRequestStateMachine.IsTerminal(DeploymentRequestStatus.Rejected).Should().BeTrue();
        DeploymentRequestStateMachine.IsTerminal(DeploymentRequestStatus.Cancelled).Should().BeTrue();
        DeploymentRequestStateMachine.IsTerminal(DeploymentRequestStatus.Expired).Should().BeTrue();
    }

    [Fact]
    public void Cannot_transition_from_a_terminal_status()
    {
        var request = CreateDraftRequest();
        request.TransitionTo(DeploymentRequestStatus.Cancelled, Actor(), expectedVersion: 1);

        var act = () => request.TransitionTo(DeploymentRequestStatus.RequirementsComplete, Actor(), expectedVersion: 2);

        act.Should().Throw<IllegalDeploymentRequestTransitionException>();
    }

    [Fact]
    public void Full_happy_path_reaches_evidence_finalized()
    {
        var request = CreateDraftRequest();
        var actor = Actor();

        var path = new[]
        {
            DeploymentRequestStatus.RequirementsComplete,
            DeploymentRequestStatus.PatternSelected,
            DeploymentRequestStatus.ParametersRendered,
            DeploymentRequestStatus.ValidationInProgress,
            DeploymentRequestStatus.ValidationPassed,
            DeploymentRequestStatus.PlanGenerated,
            DeploymentRequestStatus.AwaitingApproval,
            DeploymentRequestStatus.Approved,
            DeploymentRequestStatus.DeploymentQueued,
            DeploymentRequestStatus.Deploying,
            DeploymentRequestStatus.Deployed,
            DeploymentRequestStatus.EvidenceFinalized,
        };

        foreach (var next in path)
        {
            request.TransitionTo(next, actor, expectedVersion: request.Version);
        }

        request.Status.Should().Be(DeploymentRequestStatus.EvidenceFinalized);
        request.DomainEvents.Should().HaveCount(path.Length);
    }

    [Fact]
    public void Validation_failed_can_return_to_parameters_rendered_for_correction()
    {
        var request = CreateDraftRequest();
        var actor = Actor();
        request.TransitionTo(DeploymentRequestStatus.RequirementsComplete, actor, request.Version);
        request.TransitionTo(DeploymentRequestStatus.PatternSelected, actor, request.Version);
        request.TransitionTo(DeploymentRequestStatus.ParametersRendered, actor, request.Version);
        request.TransitionTo(DeploymentRequestStatus.ValidationInProgress, actor, request.Version);

        request.TransitionTo(DeploymentRequestStatus.ValidationFailed, actor, request.Version);
        request.TransitionTo(DeploymentRequestStatus.ParametersRendered, actor, request.Version);

        request.Status.Should().Be(DeploymentRequestStatus.ParametersRendered);
    }

    [Fact]
    public void Stale_version_throws_concurrency_conflict()
    {
        var request = CreateDraftRequest();

        var act = () => request.TransitionTo(DeploymentRequestStatus.RequirementsComplete, Actor(), expectedVersion: 99);

        act.Should().Throw<ConcurrencyConflictException>();
    }
}
