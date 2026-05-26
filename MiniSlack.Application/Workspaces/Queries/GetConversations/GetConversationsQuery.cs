using MediatR;

namespace MiniSlack.Application.Workspaces.Queries.GetConversations;

public sealed record GetConversationsQuery(
    Guid UserId,
    Guid WorkspaceId) : IRequest<IReadOnlyList<ConversationSummary>>;
