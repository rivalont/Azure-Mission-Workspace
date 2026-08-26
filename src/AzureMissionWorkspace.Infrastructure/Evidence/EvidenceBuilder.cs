using System.Security.Cryptography;
using System.Text;
using AzureMissionWorkspace.Domain.Entities;

namespace AzureMissionWorkspace.Infrastructure.Evidence;

/// <summary>
/// Builds <see cref="DeploymentEvidence"/> packages from named artifact contents, computing a
/// SHA-256 integrity hash for each artifact. Callers are responsible for redacting secret values
/// from artifact content before it is passed here -- this builder never inspects content for
/// secrets itself.
/// </summary>
public sealed class EvidenceBuilder
{
    private readonly Dictionary<string, EvidenceArtifactReference> _artifacts = new(StringComparer.OrdinalIgnoreCase);

    public EvidenceBuilder AddArtifact(string name, string content, string storageUri)
    {
        var hash = ComputeSha256(content);
        _artifacts[name] = new EvidenceArtifactReference(name, hash, storageUri, DateTimeOffset.UtcNow);
        return this;
    }

    public DeploymentEvidence Build(Guid deploymentRequestId)
        => new(Guid.NewGuid(), deploymentRequestId, _artifacts, DateTimeOffset.UtcNow);

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
