using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Worker;

/// <summary>
/// The well-known, non-human actor identity recorded against deployment-request transitions that
/// the Worker performs on behalf of automated pipeline observation (never a deployment decision).
/// This never replaces the original human requestor or approver in the audit trail -- it is
/// recorded alongside them as the system component that observed pipeline or time-based state.
/// </summary>
public static class WorkerActor
{
    public static readonly ActorIdentity Identity = new(
        ObjectId: "system-worker",
        DisplayName: "Azure Mission Workspace Worker",
        UserPrincipalName: "system-worker@azure-mission-workspace.local",
        Roles: ["PlatformEngineer"]);
}
