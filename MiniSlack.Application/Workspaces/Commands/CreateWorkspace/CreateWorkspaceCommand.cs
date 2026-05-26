using MediatR;

namespace MiniSlack.Application.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(
    Guid UserId,
    CreateWorkspaceRequest Request) : IRequest<WorkspaceSummary>;
