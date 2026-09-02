using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.PolicyEngine.Abstractions;

namespace AzureMissionWorkspace.PolicyEngine.Tests;

internal static class PolicyTestData
{
    public static PolicyRuleContext CreateContext(
        IReadOnlyDictionary<string, string>? parameters = null,
        ServicePattern? pattern = null,
        EnvironmentProfile? environmentProfile = null,
        DeploymentPlan? plan = null)
    {
        environmentProfile ??= CreateEnvironmentProfile();
        pattern ??= CreatePattern();

        var request = new DeploymentRequest(
            DeploymentRequestId.New(),
            CorrelationId.New(),
            new ActorIdentity("requestor-1", "Requestor", "requestor@example.com", ["DeploymentRequestor"]),
            environmentProfile.Id,
            "Deploy a workload");

        request.TransitionTo(DeploymentRequestStatus.RequirementsComplete, request.Requestor, request.Version);
        request.SelectServicePattern(pattern.Id, pattern.Version, request.Requestor, request.Version);

        var deploymentParameters = new DeploymentParameters(pattern.SecretInputs);
        foreach (var (name, value) in parameters ?? CreateCompliantParameters())
        {
            deploymentParameters.Set(name, value);
        }

        request.RenderParameters(deploymentParameters, request.Requestor, request.Version);

        return new PolicyRuleContext(request, pattern, environmentProfile, plan);
    }

    public static EnvironmentProfile CreateEnvironmentProfile(
        EnvironmentType environmentType = EnvironmentType.Development,
        AzureCloud cloud = AzureCloud.AzureCommercial,
        IReadOnlyCollection<string>? allowedLocations = null,
        IReadOnlyCollection<string>? allowedPatterns = null,
        IReadOnlyDictionary<string, string>? requiredTags = null)
    {
        return new EnvironmentProfile(
            new EnvironmentProfileId($"profile-{environmentType.ToString().ToLowerInvariant()}"),
            cloud,
            environmentType,
            "tenant",
            "subscription",
            allowedLocations?.FirstOrDefault() ?? "eastus",
            allowedLocations?.ToArray() ?? ["eastus", "eastus2"],
            (allowedPatterns ?? ["internal-web-api"]).Select(static x => new ServicePatternId(x)).ToArray(),
            requiredTags ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["costCenter"] = "required",
                ["owner"] = "required",
                ["environment"] = "required",
                ["dataClassification"] = "required",
            },
            requiresApprovalForProduction: environmentType == EnvironmentType.Production);
    }

    public static ServicePattern CreatePattern(
        string id = "internal-web-api",
        DeploymentScope scope = DeploymentScope.ResourceGroup,
        IReadOnlyCollection<ServicePatternInput>? requiredInputs = null,
        IReadOnlyCollection<string>? secretInputs = null,
        IReadOnlyCollection<string>? moduleReferences = null,
        IReadOnlyCollection<AzureCloud>? supportedClouds = null,
        IReadOnlyCollection<EnvironmentType>? supportedEnvironmentTypes = null)
    {
        return new ServicePattern(
            new ServicePatternId(id),
            new ServicePatternVersion("1.0.0"),
            "Pattern",
            "Pattern description",
            DeploymentStrategyType.ArmTemplate,
            scope,
            supportedClouds?.ToArray() ?? [AzureCloud.AzureCommercial, AzureCloud.AzureGovernment],
            supportedEnvironmentTypes?.ToArray() ?? [EnvironmentType.Development, EnvironmentType.Test, EnvironmentType.Staging, EnvironmentType.Production],
            ["eastus", "eastus2", "usgovvirginia"],
            requiredInputs ?? CreateRequiredInputs(),
            [new ServicePatternInput("skuName", "string", "SKU", false, false)],
            secretInputs ?? ["apiSecret"],
            moduleReferences ?? ["br:missionworkspace.azurecr.io/bicep/modules/app-service:1.0.0"],
            isDeprecated: false);
    }

    public static Dictionary<string, string> CreateCompliantParameters() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["workloadName"] = "amw-api",
        ["location"] = "eastus",
        ["environment"] = "dev",
        ["costCenter"] = "CC100",
        ["owner"] = "owner@example.com",
        ["dataClassification"] = "Public",
        ["managedIdentityEnabled"] = "true",
        ["publicNetworkAccess"] = "Disabled",
        ["logAnalyticsWorkspaceResourceId"] = "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.OperationalInsights/workspaces/law",
        ["sku"] = "Standard_LRS",
        ["apiSecret"] = "https://vault.example.net/secrets/api",
    };

    public static DeploymentPlan CreatePlan(Guid requestId, PlanChangeType changeType, DeploymentRisk risk)
    {
        return new DeploymentPlan(
            Guid.NewGuid(),
            requestId,
            [new DeploymentPlanChange("/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/sa1", "Microsoft.Storage/storageAccounts", changeType, risk, [])],
            risk,
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyCollection<ServicePatternInput> CreateRequiredInputs()
        => [
            new ServicePatternInput("workloadName", "string", "Workload name", true, false),
            new ServicePatternInput("location", "string", "Location", true, false),
            new ServicePatternInput("environment", "string", "Environment", true, false),
            new ServicePatternInput("costCenter", "string", "Cost center", true, false),
            new ServicePatternInput("owner", "string", "Owner", true, false),
            new ServicePatternInput("dataClassification", "string", "Classification", true, false),
            new ServicePatternInput("logAnalyticsWorkspaceResourceId", "string", "Workspace", true, false),
        ];
}
