using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Queries.GetWorkspaces;

public sealed class GetWorkspacesQueryHandler
    : IRequestHandler<GetWorkspacesQuery, IReadOnlyList<WorkspaceSummary>>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IWorkspaceReadStore _readStore;

    public GetWorkspacesQueryHandler(
        IWorkspaceCommandStore commandStore,
        IWorkspaceReadStore readStore)
    {
        _commandStore = commandStore;
        _readStore = readStore;
    }

    public async Task<IReadOnlyList<WorkspaceSummary>> Handle(
        GetWorkspacesQuery query,
        CancellationToken cancellationToken)
    {
        await _commandStore.EnsureDefaultWorkspaceAsync(query.UserId, cancellationToken);

        return await _readStore.GetWorkspacesAsync(query.UserId, cancellationToken);
    }
}
