using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Domain.Entities;

/// <summary>Describes a single required or optional input accepted by a service pattern.</summary>
public sealed record ServicePatternInput(string Name, string Type, string Description, bool IsRequired, bool IsSecret, string? DefaultValue = null);

/// <summary>
/// An approved, versioned Azure capability that users can request. The domain-level
/// representation captures only the facts needed for state transitions, eligibility, and policy
/// evaluation; the full authoring contract (bicep references, diagnostics, etc.) lives in the
/// ServicePatterns project's descriptor model.
/// </summary>
public sealed class ServicePattern
{
    public ServicePattern(
        ServicePatternId id,
        ServicePatternVersion version,
        string displayName,
        string description,
        DeploymentStrategyType deploymentStrategy,
        DeploymentScope scope,
        IReadOnlyCollection<AzureCloud> supportedClouds,
        IReadOnlyCollection<EnvironmentType> supportedEnvironmentTypes,
        IReadOnlyCollection<string> supportedRegions,
        IReadOnlyCollection<ServicePatternInput> requiredInputs,
        IReadOnlyCollection<ServicePatternInput> optionalInputs,
        IReadOnlyCollection<string> secretInputs,
        IReadOnlyCollection<string>? moduleReferences = null,
        bool isDeprecated = false)
    {
        Id = id;
        Version = version;
        DisplayName = displayName;
        Description = description;
        DeploymentStrategy = deploymentStrategy;
        Scope = scope;
        SupportedClouds = supportedClouds;
        SupportedEnvironmentTypes = supportedEnvironmentTypes;
        SupportedRegions = supportedRegions;
        RequiredInputs = requiredInputs;
        OptionalInputs = optionalInputs;
        SecretInputs = secretInputs;
        ModuleReferences = moduleReferences ?? [];
        IsDeprecated = isDeprecated;
    }

    public ServicePatternId Id { get; }

    public ServicePatternVersion Version { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public DeploymentStrategyType DeploymentStrategy { get; }

    public DeploymentScope Scope { get; }

    public IReadOnlyCollection<AzureCloud> SupportedClouds { get; }

    public IReadOnlyCollection<EnvironmentType> SupportedEnvironmentTypes { get; }

    public IReadOnlyCollection<string> SupportedRegions { get; }

    public IReadOnlyCollection<ServicePatternInput> RequiredInputs { get; }

    public IReadOnlyCollection<ServicePatternInput> OptionalInputs { get; }

    public IReadOnlyCollection<string> SecretInputs { get; }

    public IReadOnlyCollection<string> ModuleReferences { get; }

    public bool IsDeprecated { get; }

    public bool SupportsCloud(AzureCloud cloud) => SupportedClouds.Contains(cloud);

    public bool SupportsEnvironmentType(EnvironmentType environmentType) => SupportedEnvironmentTypes.Contains(environmentType);
}

/// <summary>
/// Organization-controlled settings for a target cloud, tenant, subscription, network, region,
/// policy baseline, and environment. Environment profiles are authored and maintained by platform
/// administrators, never by ordinary deployment requestors.
/// </summary>
public sealed class EnvironmentProfile
{
    public EnvironmentProfile(
        EnvironmentProfileId id,
        AzureCloud cloud,
        EnvironmentType environmentType,
        string tenantIdPlaceholder,
        string subscriptionIdPlaceholder,
        string defaultLocation,
        IReadOnlyCollection<string> allowedLocations,
        IReadOnlyCollection<ServicePatternId> allowedServicePatterns,
        IReadOnlyDictionary<string, string> requiredTags,
        bool requiresApprovalForProduction)
    {
        Id = id;
        Cloud = cloud;
        EnvironmentType = environmentType;
        TenantIdPlaceholder = tenantIdPlaceholder;
        SubscriptionIdPlaceholder = subscriptionIdPlaceholder;
        DefaultLocation = defaultLocation;
        AllowedLocations = allowedLocations;
        AllowedServicePatterns = allowedServicePatterns;
        RequiredTags = requiredTags;
        RequiresApprovalForProduction = requiresApprovalForProduction;
    }

    public EnvironmentProfileId Id { get; }

    public AzureCloud Cloud { get; }

    public EnvironmentType EnvironmentType { get; }

    public string TenantIdPlaceholder { get; }

    public string SubscriptionIdPlaceholder { get; }

    public string DefaultLocation { get; }

    public IReadOnlyCollection<string> AllowedLocations { get; }

    public IReadOnlyCollection<ServicePatternId> AllowedServicePatterns { get; }

    public IReadOnlyDictionary<string, string> RequiredTags { get; }

    public bool RequiresApprovalForProduction { get; }

    public bool AllowsServicePattern(ServicePatternId patternId) => AllowedServicePatterns.Contains(patternId);

    public bool IsProtected => EnvironmentType == EnvironmentType.Production;
}
