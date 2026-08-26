using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.ServicePatterns.Descriptors;

namespace AzureMissionWorkspace.ServicePatterns.Loading;

/// <summary>Maps a parsed <see cref="ServicePatternDescriptor"/> to the domain-level <see cref="ServicePattern"/> entity.</summary>
public static class ServicePatternDescriptorMapper
{
    public static ServicePattern ToDomain(ServicePatternDescriptor descriptor)
    {
        return new ServicePattern(
            new ServicePatternId(descriptor.Id),
            new ServicePatternVersion(descriptor.Version),
            descriptor.DisplayName,
            descriptor.Description,
            Enum.Parse<DeploymentStrategyType>(descriptor.DeploymentStrategy, ignoreCase: true),
            DeploymentScope.ResourceGroup,
            descriptor.SupportedClouds.Select(c => Enum.Parse<AzureCloud>(c, ignoreCase: true)).ToArray(),
            descriptor.SupportedEnvironmentTypes.Select(e => Enum.Parse<EnvironmentType>(e, ignoreCase: true)).ToArray(),
            descriptor.SupportedRegions,
            descriptor.RequiredInputs.Select(i => ToDomainInput(i, isRequired: true)).ToArray(),
            descriptor.OptionalInputs.Select(i => ToDomainInput(i, isRequired: false)).ToArray(),
            descriptor.SecretInputs.Select(i => i.Name).ToArray(),
            descriptor.ModuleReferences.Select(m => $"{m.Reference}:{m.Version}").ToArray(),
            isDeprecated: string.Equals(descriptor.Status, "Deprecated", StringComparison.OrdinalIgnoreCase) || descriptor.Deprecation is not null);
    }

    private static ServicePatternInput ToDomainInput(ServicePatternInputDescriptor input, bool isRequired)
    {
        return new ServicePatternInput(input.Name, input.Type, input.Description, IsRequired: isRequired, IsSecret: false);
    }
}
