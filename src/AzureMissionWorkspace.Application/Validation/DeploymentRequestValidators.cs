using AzureMissionWorkspace.Application.Dtos;
using FluentValidation;

namespace AzureMissionWorkspace.Application.Validation;

/// <summary>Validates the shape of a request to create a new deployment request draft.</summary>
public sealed class CreateDeploymentRequestInputValidator : AbstractValidator<CreateDeploymentRequestInput>
{
    public CreateDeploymentRequestInputValidator()
    {
        RuleFor(x => x.RequestorObjectId).NotEmpty();
        RuleFor(x => x.RequestorDisplayName).NotEmpty();
        RuleFor(x => x.RequestorUpn).NotEmpty().EmailAddress();
        RuleFor(x => x.EnvironmentProfileId).NotEmpty();
        RuleFor(x => x.NaturalLanguageRequest)
            .NotEmpty()
            .MaximumLength(4000)
            .WithMessage("Deployment request descriptions must be 4000 characters or fewer.");
    }
}

/// <summary>Validates a request to select a service pattern for an existing deployment request.</summary>
public sealed class SelectServicePatternInputValidator : AbstractValidator<SelectServicePatternInput>
{
    public SelectServicePatternInputValidator()
    {
        RuleFor(x => x.DeploymentRequestId).NotEmpty();
        RuleFor(x => x.ServicePatternId).NotEmpty();
        RuleFor(x => x.ServicePatternVersion).NotEmpty().Matches(@"^\d+\.\d+\.\d+$").WithMessage("Service pattern version must be a semantic version (major.minor.patch).");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}

/// <summary>Validates a request to record an approval or rejection decision.</summary>
public sealed class RecordApprovalDecisionInputValidator : AbstractValidator<RecordApprovalDecisionInput>
{
    public RecordApprovalDecisionInputValidator()
    {
        RuleFor(x => x.DeploymentRequestId).NotEmpty();
        RuleFor(x => x.ApprovalRequirementId).NotEmpty();
        RuleFor(x => x.ApproverObjectId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
    }
}
