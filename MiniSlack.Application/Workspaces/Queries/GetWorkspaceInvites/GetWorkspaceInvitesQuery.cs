using MediatR;

namespace MiniSlack.Application.Workspaces.Queries.GetWorkspaceInvites;

public sealed record GetWorkspaceInvitesQuery(
    Guid UserId,
    Guid WorkspaceId) : IRequest<IReadOnlyList<WorkspaceInviteSummary>>;
