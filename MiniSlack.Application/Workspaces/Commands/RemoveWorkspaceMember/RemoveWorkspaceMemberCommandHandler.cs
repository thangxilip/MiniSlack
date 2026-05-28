using MediatR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.RemoveWorkspaceMember;

public sealed class RemoveWorkspaceMemberCommandHandler
    : IRequestHandler<RemoveWorkspaceMemberCommand, RemovedWorkspaceMemberSummary>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public RemoveWorkspaceMemberCommandHandler(
        IWorkspaceCommandStore commandStore,
        IRealtimeNotifier realtimeNotifier)
    {
        _commandStore = commandStore;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<RemovedWorkspaceMemberSummary> Handle(
        RemoveWorkspaceMemberCommand command,
        CancellationToken cancellationToken)
    {
        var member = await _commandStore.RemoveWorkspaceMemberAsync(
            command.UserId,
            command.WorkspaceId,
            command.TargetUserId,
            cancellationToken);

        await _realtimeNotifier.WorkspaceMemberRemovedAsync(member, cancellationToken);

        return member;
    }
}
