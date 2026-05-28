using MediatR;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;

namespace MiniSlack.Application.Workspaces.Commands.AcceptWorkspaceInvite;

public sealed class AcceptWorkspaceInviteCommandHandler
    : IRequestHandler<AcceptWorkspaceInviteCommand, AcceptWorkspaceInviteResult>
{
    private readonly IWorkspaceCommandStore _commandStore;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AcceptWorkspaceInviteCommandHandler(
        IWorkspaceCommandStore commandStore,
        IRealtimeNotifier realtimeNotifier)
    {
        _commandStore = commandStore;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<AcceptWorkspaceInviteResult> Handle(
        AcceptWorkspaceInviteCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _commandStore.AcceptWorkspaceInviteAsync(
            command.UserId,
            command.Request,
            cancellationToken);

        await _realtimeNotifier.WorkspaceMemberAddedAsync(
            result.Member,
            result.Workspace.Id,
            cancellationToken);

        return result;
    }
}
