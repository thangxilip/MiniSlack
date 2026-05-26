using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.CreateMessage;

public sealed record CreateMessageCommand(
    Guid UserId,
    Guid ConversationId,
    CreateMessageRequest Request) : IRequest<MessageSummary>;
