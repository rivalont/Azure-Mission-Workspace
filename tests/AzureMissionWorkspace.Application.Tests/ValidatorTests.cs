using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Application.Validation;
using AzureMissionWorkspace.Domain.Enums;
using FluentAssertions;

namespace AzureMissionWorkspace.Application.Tests;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateDeploymentRequestInputValidator_accepts_valid_input()
    {
        var validator = new CreateDeploymentRequestInputValidator();

        var result = validator.Validate(new CreateDeploymentRequestInput(
            "requestor-1",
            "Requestor One",
            "requestor@example.com",
            "azure-commercial-development",
            "Deploy an internal API"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateDeploymentRequestInputValidator_rejects_invalid_input()
    {
        var validator = new CreateDeploymentRequestInputValidator();

        var result = validator.Validate(new CreateDeploymentRequestInput(
            "",
            "",
            "not-an-email",
            "",
            new string('x', 4001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(static e => e.PropertyName).Should().Contain(["RequestorObjectId", "RequestorDisplayName", "RequestorUpn", "EnvironmentProfileId", "NaturalLanguageRequest"]);
    }

    [Fact]
    public void SelectServicePatternInputValidator_accepts_valid_input()
    {
        var validator = new SelectServicePatternInputValidator();

        var result = validator.Validate(new SelectServicePatternInput(Guid.NewGuid(), "internal-web-api", "1.2.3", 2));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SelectServicePatternInputValidator_rejects_non_semantic_version()
    {
        var validator = new SelectServicePatternInputValidator();

        var result = validator.Validate(new SelectServicePatternInput(Guid.NewGuid(), "internal-web-api", "latest", 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(static e => e.PropertyName).Should().Contain(["ServicePatternVersion", "ExpectedVersion"]);
    }

    [Fact]
    public void RecordApprovalDecisionInputValidator_accepts_valid_input()
    {
        var validator = new RecordApprovalDecisionInputValidator();

        var result = validator.Validate(new RecordApprovalDecisionInput(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "approver-1",
            ApprovalStatus.Approved,
            "Approved"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordApprovalDecisionInputValidator_rejects_missing_approver()
    {
        var validator = new RecordApprovalDecisionInputValidator();

        var result = validator.Validate(new RecordApprovalDecisionInput(
            Guid.Empty,
            Guid.Empty,
            "",
            ApprovalStatus.Approved,
            null));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(static e => e.PropertyName).Should().Contain(["DeploymentRequestId", "ApprovalRequirementId", "ApproverObjectId"]);
    }
}
