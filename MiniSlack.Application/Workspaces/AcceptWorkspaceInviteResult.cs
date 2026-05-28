namespace MiniSlack.Application.Workspaces;

public sealed record AcceptWorkspaceInviteResult(
    WorkspaceSummary Workspace,
    WorkspaceMemberSummary Member);
