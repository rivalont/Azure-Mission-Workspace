using AzureMissionWorkspace.ServicePatterns.Descriptors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AzureMissionWorkspace.ServicePatterns.Loading;

/// <summary>Parses a single <c>service-pattern.yaml</c> descriptor file into a strongly typed model.</summary>
public sealed class ServicePatternDescriptorParser
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public ServicePatternDescriptor Parse(string yamlContent)
    {
        return _deserializer.Deserialize<ServicePatternDescriptor>(yamlContent)
            ?? throw new InvalidOperationException("The service-pattern.yaml content could not be parsed.");
    }

    public ServicePatternDescriptor ParseFile(string filePath)
    {
        return Parse(File.ReadAllText(filePath));
    }
}
