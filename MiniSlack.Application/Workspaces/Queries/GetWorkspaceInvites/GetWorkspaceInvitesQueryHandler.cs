using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Queries.GetWorkspaceInvites;

public sealed class GetWorkspaceInvitesQueryHandler
    : IRequestHandler<GetWorkspaceInvitesQuery, IReadOnlyList<WorkspaceInviteSummary>>
{
    private readonly IWorkspaceReadStore _readStore;

    public GetWorkspaceInvitesQueryHandler(IWorkspaceReadStore readStore)
    {
        _readStore = readStore;
    }

    public Task<IReadOnlyList<WorkspaceInviteSummary>> Handle(
        GetWorkspaceInvitesQuery query,
        CancellationToken cancellationToken)
    {
        return _readStore.GetWorkspaceInvitesAsync(
            query.UserId,
            query.WorkspaceId,
            cancellationToken);
    }
}
