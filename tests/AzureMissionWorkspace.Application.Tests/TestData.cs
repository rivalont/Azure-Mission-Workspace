using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.Tests;

internal static class TestData
{
    public static EnvironmentProfile CreateEnvironmentProfile(
        string id = "env-dev",
        AzureCloud cloud = AzureCloud.AzureCommercial,
        EnvironmentType environmentType = EnvironmentType.Development,
        IReadOnlyCollection<string>? allowedServicePatterns = null,
        IReadOnlyCollection<string>? allowedLocations = null,
        IReadOnlyDictionary<string, string>? requiredTags = null)
    {
        return new EnvironmentProfile(
            new EnvironmentProfileId(id),
            cloud,
            environmentType,
            "tenant",
            "subscription",
            allowedLocations?.FirstOrDefault() ?? "eastus",
            allowedLocations?.ToArray() ?? ["eastus", "eastus2"],
            (allowedServicePatterns ?? ["internal-web-api"]).Select(static x => new ServicePatternId(x)).ToArray(),
            requiredTags ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["costCenter"] = "required",
                ["owner"] = "required",
                ["environment"] = "required",
                ["dataClassification"] = "required",
            },
            requiresApprovalForProduction: environmentType == EnvironmentType.Production);
    }

    public static ServicePattern CreateServicePattern(
        string id = "internal-web-api",
        string version = "1.0.0",
        IReadOnlyCollection<ServicePatternInput>? requiredInputs = null,
        IReadOnlyCollection<ServicePatternInput>? optionalInputs = null,
        IReadOnlyCollection<string>? secretInputs = null,
        AzureCloud[]? supportedClouds = null,
        EnvironmentType[]? supportedEnvironmentTypes = null,
        bool isDeprecated = false)
    {
        return new ServicePattern(
            new ServicePatternId(id),
            new ServicePatternVersion(version),
            "Pattern",
            "Pattern description",
            DeploymentStrategyType.ArmTemplate,
            DeploymentScope.ResourceGroup,
            supportedClouds ?? [AzureCloud.AzureCommercial, AzureCloud.AzureGovernment],
            supportedEnvironmentTypes ?? [EnvironmentType.Development, EnvironmentType.Test, EnvironmentType.Staging, EnvironmentType.Production],
            ["eastus", "eastus2", "usgovvirginia"],
            requiredInputs ?? CreateDefaultRequiredInputs(),
            optionalInputs ?? [new ServicePatternInput("skuName", "string", "SKU", false, false)],
            secretInputs ?? ["apiSecret"],
            moduleReferences: ["br:missionworkspace.azurecr.io/bicep/modules/app-service:1.0.0"],
            isDeprecated);
    }

    public static DeploymentRequest CreateRequest(
        EnvironmentProfile environmentProfile,
        DeploymentRequestStatus status = DeploymentRequestStatus.RequirementsComplete,
        ServicePattern? pattern = null,
        IReadOnlyDictionary<string, string>? parameterValues = null,
        string requestorObjectId = "requestor-1")
    {
        var request = new DeploymentRequest(
            DeploymentRequestId.New(),
            CorrelationId.New(),
            new ActorIdentity(requestorObjectId, "Requestor", "requestor@example.com", ["DeploymentRequestor"]),
            environmentProfile.Id,
            "Deploy an internal API");

        if (status == DeploymentRequestStatus.Draft)
        {
            return request;
        }

        request.TransitionTo(DeploymentRequestStatus.RequirementsComplete, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.RequirementsComplete)
        {
            return request;
        }

        pattern ??= CreateServicePattern();
        request.SelectServicePattern(pattern.Id, pattern.Version, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.PatternSelected)
        {
            return request;
        }

        var parameters = new DeploymentParameters(pattern.SecretInputs);
        foreach (var (name, value) in parameterValues ?? CreateValidParameterValues())
        {
            parameters.Set(name, value);
        }

        request.RenderParameters(parameters, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.ParametersRendered)
        {
            return request;
        }

        request.TransitionTo(DeploymentRequestStatus.ValidationInProgress, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.ValidationInProgress)
        {
            return request;
        }

        var validationTerminal = status is DeploymentRequestStatus.ValidationPassed or DeploymentRequestStatus.ValidationFailed
            ? status
            : DeploymentRequestStatus.ValidationPassed;
        request.TransitionTo(validationTerminal, request.Requestor, request.Version);

        if (status is DeploymentRequestStatus.ValidationPassed or DeploymentRequestStatus.ValidationFailed)
        {
            return request;
        }

        request.TransitionTo(DeploymentRequestStatus.PlanGenerated, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.PlanGenerated)
        {
            return request;
        }

        request.TransitionTo(DeploymentRequestStatus.AwaitingApproval, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.AwaitingApproval)
        {
            return request;
        }

        if (status == DeploymentRequestStatus.Rejected)
        {
            request.TransitionTo(DeploymentRequestStatus.Rejected, request.Requestor, request.Version);
            return request;
        }

        request.TransitionTo(DeploymentRequestStatus.Approved, request.Requestor, request.Version);

        if (status == DeploymentRequestStatus.Approved)
        {
            return request;
        }

        request.TransitionTo(DeploymentRequestStatus.DeploymentQueued, request.Requestor, request.Version);

        return request;
    }

    public static Dictionary<string, string> CreateValidParameterValues() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["workloadName"] = "api-workload",
        ["location"] = "eastus",
        ["environment"] = "dev",
        ["costCenter"] = "CC100",
        ["owner"] = "platform@example.com",
        ["dataClassification"] = "Internal",
        ["logAnalyticsWorkspaceResourceId"] = "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.OperationalInsights/workspaces/law",
        ["containerImage"] = "example.azurecr.io/api:1.0.0",
        ["apiSecret"] = "https://vault.vault.azure.net/secrets/api",
    };

    public static DeploymentPlan CreatePlan(
        Guid deploymentRequestId,
        DeploymentRisk risk = DeploymentRisk.Low,
        PlanChangeType changeType = PlanChangeType.Create)
    {
        var change = new DeploymentPlanChange(
            "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/sa1",
            "Microsoft.Storage/storageAccounts",
            changeType,
            risk,
            []);

        return new DeploymentPlan(Guid.NewGuid(), deploymentRequestId, [change], risk, DateTimeOffset.UtcNow);
    }

    public static DeploymentEvidence CreateEvidence(Guid deploymentRequestId)
    {
        return new DeploymentEvidence(
            Guid.NewGuid(),
            deploymentRequestId,
            new Dictionary<string, EvidenceArtifactReference>(StringComparer.OrdinalIgnoreCase)
            {
                ["plan.json"] = new("plan.json", "abc123", "memory://plan.json", DateTimeOffset.UtcNow),
            },
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyCollection<ServicePatternInput> CreateDefaultRequiredInputs()
        => [
            new ServicePatternInput("workloadName", "string", "Workload name", true, false),
            new ServicePatternInput("location", "string", "Location", true, false),
            new ServicePatternInput("environment", "string", "Environment", true, false),
            new ServicePatternInput("costCenter", "string", "Cost center", true, false),
            new ServicePatternInput("owner", "string", "Owner", true, false),
            new ServicePatternInput("dataClassification", "string", "Classification", true, false),
            new ServicePatternInput("logAnalyticsWorkspaceResourceId", "string", "LAW", true, false),
            new ServicePatternInput("containerImage", "string", "Image", true, false),
        ];
}
