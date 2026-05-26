using MediatR;

namespace MiniSlack.Application.Workspaces.Queries.GetWorkspaceMembers;

public sealed record GetWorkspaceMembersQuery(
    Guid UserId,
    Guid WorkspaceId) : IRequest<IReadOnlyList<WorkspaceMemberSummary>>;
