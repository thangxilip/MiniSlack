using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Queries.GetWorkspaceMembers;

public sealed class GetWorkspaceMembersQueryHandler
    : IRequestHandler<GetWorkspaceMembersQuery, IReadOnlyList<WorkspaceMemberSummary>>
{
    private readonly IWorkspaceReadStore _readStore;

    public GetWorkspaceMembersQueryHandler(IWorkspaceReadStore readStore)
    {
        _readStore = readStore;
    }

    public Task<IReadOnlyList<WorkspaceMemberSummary>> Handle(
        GetWorkspaceMembersQuery query,
        CancellationToken cancellationToken)
    {
        return _readStore.GetWorkspaceMembersAsync(
            query.UserId,
            query.WorkspaceId,
            cancellationToken);
    }
}
