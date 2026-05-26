using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.StartDirectMessage;

public sealed record StartDirectMessageCommand(
    Guid UserId,
    Guid WorkspaceId,
    StartDirectMessageRequest Request) : IRequest<ConversationSummary>;
