namespace MiniSlack.Application.Workspaces;

public sealed record StartDirectMessageRequest(
    Guid TargetUserId);
