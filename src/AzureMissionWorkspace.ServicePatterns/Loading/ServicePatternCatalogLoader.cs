using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.ServicePatterns.Descriptors;

namespace AzureMissionWorkspace.ServicePatterns.Loading;

/// <summary>A parsed service pattern paired with the raw descriptor it was parsed from and the directory it was loaded from.</summary>
public sealed record LoadedServicePattern(ServicePattern Pattern, ServicePatternDescriptor Descriptor, string DirectoryPath);

/// <summary>
/// Loads the approved service-pattern catalog from a directory tree such as the repository's
/// <c>service-patterns/</c> folder. Each immediate subdirectory is expected to contain a
/// <c>service-pattern.yaml</c> descriptor, an <c>input-schema.json</c> file, and Bicep artifacts.
/// </summary>
public sealed class ServicePatternCatalogLoader
{
    private readonly ServicePatternDescriptorParser _parser = new();

    public IReadOnlyCollection<LoadedServicePattern> LoadFromDirectory(string catalogRootPath)
    {
        if (!Directory.Exists(catalogRootPath))
        {
            return [];
        }

        var results = new List<LoadedServicePattern>();

        foreach (var directory in Directory.EnumerateDirectories(catalogRootPath).OrderBy(d => d, StringComparer.Ordinal))
        {
            var descriptorPath = Path.Combine(directory, "service-pattern.yaml");
            if (!File.Exists(descriptorPath))
            {
                continue;
            }

            var descriptor = _parser.ParseFile(descriptorPath);
            var pattern = ServicePatternDescriptorMapper.ToDomain(descriptor);
            results.Add(new LoadedServicePattern(pattern, descriptor, directory));
        }

        return results;
    }
}
