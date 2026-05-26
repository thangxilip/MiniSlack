using MiniSlack.Domain.Conversations;

namespace MiniSlack.Application.Workspaces;

public sealed record CreateConversationRequest(
    string Name,
    string? Description,
    ConversationType Type,
    bool IsPrivate);
