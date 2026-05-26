using MiniSlack.Domain.Conversations;

namespace MiniSlack.Application.Workspaces;

public sealed record ConversationSummary(
    Guid Id,
    Guid WorkspaceId,
    ConversationType Type,
    string Name,
    string? Description,
    bool IsPrivate,
    int MemberCount,
    DateTimeOffset CreatedAtUtc);
