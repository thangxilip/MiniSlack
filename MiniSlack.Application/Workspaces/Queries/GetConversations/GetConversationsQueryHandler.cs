using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Queries.GetConversations;

public sealed class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationSummary>>
{
    private readonly IWorkspaceReadStore _readStore;

    public GetConversationsQueryHandler(IWorkspaceReadStore readStore)
    {
        _readStore = readStore;
    }

    public Task<IReadOnlyList<ConversationSummary>> Handle(
        GetConversationsQuery query,
        CancellationToken cancellationToken)
    {
        return _readStore.GetConversationsAsync(
            query.UserId,
            query.WorkspaceId,
            cancellationToken);
    }
}
