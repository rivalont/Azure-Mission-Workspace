namespace AzureMissionWorkspace.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.DeploymentRequest"/>. Transitions are enforced by
/// <see cref="Entities.DeploymentRequestStateMachine"/>; illegal transitions throw
/// <see cref="Exceptions.IllegalDeploymentRequestTransitionException"/>.
/// </summary>
public enum DeploymentRequestStatus
{
    Draft,
    RequirementsComplete,
    PatternSelected,
    ParametersRendered,
    ValidationInProgress,
    ValidationFailed,
    ValidationPassed,
    PlanGenerated,
    AwaitingApproval,
    Rejected,
    Approved,
    DeploymentQueued,
    Deploying,
    DeploymentFailed,
    Deployed,
    Cancelled,
    Expired,
    EvidenceFinalized,
}

/// <summary>Type of target environment for a deployment request.</summary>
public enum EnvironmentType
{
    Development,
    Test,
    Staging,
    Production,
}

/// <summary>The Azure cloud instance targeted by an environment profile.</summary>
public enum AzureCloud
{
    AzureCommercial,
    AzureGovernment,
}

/// <summary>Sensitivity classification of data handled by a deployed workload.</summary>
public enum DataClassification
{
    Public,
    Internal,
    Confidential,
    HighlyConfidential,
}

/// <summary>Deterministic, system-assigned risk classification of a deployment plan.</summary>
public enum DeploymentRisk
{
    Low,
    Medium,
    High,
    ReviewRequired,
}

/// <summary>Decision outcome for an individual approval requirement.</summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
}

/// <summary>Severity of a policy finding produced by the policy engine.</summary>
public enum PolicyFindingSeverity
{
    Info,
    Warning,
    Error,
    Blocking,
}

/// <summary>Normalized classification of a single resource-level change in a deployment plan.</summary>
public enum PlanChangeType
{
    Create,
    Modify,
    Delete,
    Replace,
    NoChange,
    Ignore,
    Unknown,
}

/// <summary>Status of an Azure DevOps pipeline execution associated with a deployment request.</summary>
public enum PipelineStatus
{
    NotStarted,
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled,
}

/// <summary>The deployment execution strategy declared by a service pattern.</summary>
public enum DeploymentStrategyType
{
    ArmTemplate,
    DeploymentStack,
}

/// <summary>The Azure Resource Manager scope at which a service pattern deploys.</summary>
public enum DeploymentScope
{
    ResourceGroup,
    Subscription,
    ManagementGroup,
    Tenant,
}
