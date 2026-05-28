namespace MiniSlack.Application.Workspaces;

public sealed record RemovedWorkspaceMemberSummary(
    Guid WorkspaceId,
    Guid UserId);
