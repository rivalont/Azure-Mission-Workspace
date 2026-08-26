using System.Collections.Concurrent;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.ServicePatterns.Descriptors;
using Microsoft.Extensions.Options;

namespace AzureMissionWorkspace.ServicePatterns.Loading;

/// <summary>Options controlling where the file-system-backed service-pattern catalog is loaded from.</summary>
public sealed class ServicePatternCatalogOptions
{
    /// <summary>Absolute or relative (to the running process) path to the <c>service-patterns/</c> directory.</summary>
    public string CatalogRootPath { get; set; } = "service-patterns";
}

/// <summary>
/// <see cref="IServicePatternRepository"/> implementation backed by the local file-system service-
/// pattern catalog. The catalog is loaded once and cached; production deployments may replace this
/// with a repository backed by a managed catalog store without any change to the Application layer.
/// </summary>
public sealed class FileSystemServicePatternRepository : IServicePatternRepository
{
    private readonly Lazy<ConcurrentDictionary<string, LoadedServicePattern>> _catalog;

    public FileSystemServicePatternRepository(IOptions<ServicePatternCatalogOptions> options)
    {
        var loader = new ServicePatternCatalogLoader();
        _catalog = new Lazy<ConcurrentDictionary<string, LoadedServicePattern>>(() =>
        {
            var loaded = loader.LoadFromDirectory(options.Value.CatalogRootPath);
            var map = new ConcurrentDictionary<string, LoadedServicePattern>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in loaded)
            {
                map[Key(entry.Pattern.Id, entry.Pattern.Version)] = entry;
            }

            return map;
        });
    }

    public Task<ServicePattern?> FindAsync(ServicePatternId id, ServicePatternVersion version, CancellationToken cancellationToken = default)
    {
        _catalog.Value.TryGetValue(Key(id, version), out var entry);
        return Task.FromResult(entry?.Pattern);
    }

    public Task<ServicePattern?> FindLatestAsync(ServicePatternId id, CancellationToken cancellationToken = default)
    {
        var latest = _catalog.Value.Values
            .Where(e => e.Pattern.Id.Equals(id))
            .OrderByDescending(e => e.Pattern.Version.Value, StringComparer.Ordinal)
            .FirstOrDefault();

        return Task.FromResult(latest?.Pattern);
    }

    public Task<IReadOnlyCollection<ServicePattern>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ServicePattern> patterns = _catalog.Value.Values.Select(e => e.Pattern).ToArray();
        return Task.FromResult(patterns);
    }

    /// <summary>
    /// Returns the raw authoring descriptor (module references, security controls, diagnostic
    /// controls, etc.) for a resolved service pattern. Used by infrastructure adapters -- such as
    /// the Bicep module resolver -- that need authoring metadata beyond the domain-level contract.
    /// </summary>
    public ServicePatternDescriptor? FindDescriptor(ServicePatternId id, ServicePatternVersion version)
    {
        _catalog.Value.TryGetValue(Key(id, version), out var entry);
        return entry?.Descriptor;
    }

    private static string Key(ServicePatternId id, ServicePatternVersion version) => $"{id.Value}@{version.Value}".ToLowerInvariant();
}
