using MiniSlack.Application.Workspaces;

namespace MiniSlack.Application.Realtime;

public interface IRealtimeNotifier
{
    Task MessageCreatedAsync(
        MessageSummary message,
        CancellationToken cancellationToken);

    Task ConversationCreatedAsync(
        ConversationSummary conversation,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken);
}
