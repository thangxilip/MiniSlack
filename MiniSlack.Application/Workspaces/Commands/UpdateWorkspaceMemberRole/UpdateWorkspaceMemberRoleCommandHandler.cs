using MediatR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.UpdateWorkspaceMemberRole;

public sealed class UpdateWorkspaceMemberRoleCommandHandler
    : IRequestHandler<UpdateWorkspaceMemberRoleCommand, WorkspaceMemberSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public UpdateWorkspaceMemberRoleCommandHandler(
        IWorkspaceCommandStore commandStore,
        IRealtimeNotifier realtimeNotifier)
    {
        _commandStore = commandStore;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<WorkspaceMemberSummary> Handle(
        UpdateWorkspaceMemberRoleCommand command,
        CancellationToken cancellationToken)
    {
        var member = await _commandStore.UpdateWorkspaceMemberRoleAsync(
            command.UserId,
            command.WorkspaceId,
            command.TargetUserId,
            command.Request,
            cancellationToken);

        await _realtimeNotifier.WorkspaceMemberRoleChangedAsync(
            member,
            command.WorkspaceId,
            cancellationToken);

        return member;
    }
}
