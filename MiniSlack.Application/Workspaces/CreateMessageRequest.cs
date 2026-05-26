namespace MiniSlack.Application.Workspaces;

public sealed record CreateMessageRequest(
    string Content,
    Guid? ParentMessageId);
