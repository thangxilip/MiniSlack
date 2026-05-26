using MediatR;

namespace MiniSlack.Application.Workspaces.Queries.GetMessages;

public sealed record GetMessagesQuery(
    Guid UserId,
    Guid ConversationId,
    DateTimeOffset? Before,
    int Limit) : IRequest<IReadOnlyList<MessageSummary>>;
