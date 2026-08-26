namespace AzureMissionWorkspace.ServicePatterns.Descriptors;

/// <summary>A single required, optional, or secret input declared by a service-pattern descriptor.</summary>
public sealed class ServicePatternInputDescriptor
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "string";

    public string Description { get; set; } = string.Empty;
}

/// <summary>A pinned reference to a reusable Bicep module wrapper.</summary>
public sealed class ModuleReferenceDescriptor
{
    public string Name { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public string Version { get; set; } = "0.0.0";
}

/// <summary>Security control defaults declared by a service pattern (managed identity, HTTPS-only, minimum TLS, public network access).</summary>
public sealed class SecurityControlsDescriptor
{
    public bool ManagedIdentity { get; set; }

    public bool HttpsOnly { get; set; }

    public string MinTlsVersion { get; set; } = "1.2";

    public string? PublicNetworkAccess { get; set; }
}

/// <summary>Network control declarations for a service pattern.</summary>
public sealed class NetworkControlsDescriptor
{
    public string Ingress { get; set; } = string.Empty;

    public string Egress { get; set; } = string.Empty;

    public bool PrivateEndpointsRequired { get; set; }

    public List<string> AllowedSubnetReferences { get; set; } = [];
}

/// <summary>Diagnostic-setting requirements declared by a service pattern.</summary>
public sealed class DiagnosticControlsDescriptor
{
    public bool Required { get; set; }

    public string LogAnalyticsWorkspaceRef { get; set; } = string.Empty;
}

/// <summary>A single environment-specific approval rule declared by a service pattern.</summary>
public sealed class ApprovalRuleDescriptor
{
    public string EnvironmentType { get; set; } = string.Empty;

    public int RequiredApprovers { get; set; } = 1;

    public string ApproverRole { get; set; } = "DeploymentApprover";
}

/// <summary>Ownership metadata for a service pattern, used for authoring accountability.</summary>
public sealed class ServicePatternOwnership
{
    public string Team { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
}

/// <summary>Documentation link metadata for a service pattern.</summary>
public sealed class DocumentationDescriptor
{
    public string Link { get; set; } = string.Empty;
}

/// <summary>Deprecation metadata; present only when a service pattern has been deprecated.</summary>
public sealed class DeprecationDescriptor
{
    public string? AnnouncedOn { get; set; }

    public string? RemovalOn { get; set; }

    public string? ReplacementPatternId { get; set; }

    public string? Reason { get; set; }
}

/// <summary>A declared output produced by a service pattern's Bicep module.</summary>
public sealed class OutputDescriptor
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Deserialized representation of a <c>service-pattern.yaml</c> descriptor. Field names mirror
/// <c>schemas/service-pattern.schema.json</c> exactly so that authoring and runtime parsing stay
/// in lockstep.
/// </summary>
public sealed class ServicePatternDescriptor
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Version { get; set; } = "0.0.0";

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "Preview";

    public string DeploymentStrategy { get; set; } = "ArmTemplate";

    public List<string> SupportedClouds { get; set; } = [];

    public List<string> SupportedEnvironmentTypes { get; set; } = [];

    public List<string> SupportedRegions { get; set; } = [];

    public List<ServicePatternInputDescriptor> RequiredInputs { get; set; } = [];

    public List<ServicePatternInputDescriptor> OptionalInputs { get; set; } = [];

    public List<ServicePatternInputDescriptor> SecretInputs { get; set; } = [];

    public List<ModuleReferenceDescriptor> ModuleReferences { get; set; } = [];

    public List<string> RequiredResourceProviders { get; set; } = [];

    public List<string> RequiredFeatures { get; set; } = [];

    public List<string> RequiredTags { get; set; } = [];

    public SecurityControlsDescriptor SecurityControls { get; set; } = new();

    public NetworkControlsDescriptor NetworkControls { get; set; } = new();

    public DiagnosticControlsDescriptor DiagnosticControls { get; set; } = new();

    public List<string> PolicyRules { get; set; } = [];

    public List<ApprovalRuleDescriptor> ApprovalRules { get; set; } = [];

    public ServicePatternOwnership Ownership { get; set; } = new();

    public DocumentationDescriptor Documentation { get; set; } = new();

    public DeprecationDescriptor? Deprecation { get; set; }

    public List<OutputDescriptor> Outputs { get; set; } = [];
}
