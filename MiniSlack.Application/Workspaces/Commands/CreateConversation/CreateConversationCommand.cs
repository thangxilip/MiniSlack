using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    Guid UserId,
    Guid WorkspaceId,
    CreateConversationRequest Request) : IRequest<ConversationSummary>;
