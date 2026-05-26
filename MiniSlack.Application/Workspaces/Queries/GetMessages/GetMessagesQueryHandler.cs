using MediatR;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Queries.GetMessages;

public sealed class GetMessagesQueryHandler
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<MessageSummary>>
{
    private readonly IWorkspaceReadStore _readStore;

    public GetMessagesQueryHandler(IWorkspaceReadStore readStore)
    {
        _readStore = readStore;
    }

    public Task<IReadOnlyList<MessageSummary>> Handle(
        GetMessagesQuery query,
        CancellationToken cancellationToken)
    {
        return _readStore.GetMessagesAsync(
            query.UserId,
            query.ConversationId,
            query.Before,
            query.Limit,
            cancellationToken);
    }
}
