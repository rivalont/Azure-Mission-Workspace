using System.Collections.Concurrent;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace AzureMissionWorkspace.Infrastructure.Persistence;

/// <summary>Options controlling where the file-system-backed environment-profile catalog is loaded from.</summary>
public sealed class EnvironmentProfileOptions
{
    /// <summary>Absolute or relative (to the running process) path to the <c>environment-profiles/</c> directory.</summary>
    public string ProfilesRootPath { get; set; } = "environment-profiles";
}

/// <summary>
/// Read-only <see cref="IEnvironmentProfileRepository"/> backed by the local file-system
/// environment-profile catalog. Environment profiles are authored and maintained by platform
/// administrators outside of this application, never by ordinary deployment requestors.
/// </summary>
public sealed class FileSystemEnvironmentProfileRepository : IEnvironmentProfileRepository
{
    private readonly Lazy<ConcurrentDictionary<string, EnvironmentProfile>> _profiles;

    public FileSystemEnvironmentProfileRepository(IOptions<EnvironmentProfileOptions> options)
    {
        _profiles = new Lazy<ConcurrentDictionary<string, EnvironmentProfile>>(() =>
        {
            var loaded = EnvironmentProfileJsonLoader.LoadFromDirectory(options.Value.ProfilesRootPath);
            var map = new ConcurrentDictionary<string, EnvironmentProfile>(StringComparer.OrdinalIgnoreCase);
            foreach (var profile in loaded)
            {
                map[profile.Id.Value] = profile;
            }

            return map;
        });
    }

    public Task<EnvironmentProfile?> FindByIdAsync(EnvironmentProfileId id, CancellationToken cancellationToken = default)
    {
        _profiles.Value.TryGetValue(id.Value, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyCollection<EnvironmentProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<EnvironmentProfile> all = _profiles.Value.Values.ToArray();
        return Task.FromResult(all);
    }
}
