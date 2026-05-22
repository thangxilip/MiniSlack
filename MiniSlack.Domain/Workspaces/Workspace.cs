using MiniSlack.Domain.Common;
using MiniSlack.Domain.Conversations;
using MiniSlack.Domain.Users;

namespace MiniSlack.Domain.Workspaces;

public sealed class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    public User? OwnerUser { get; set; }

    public ICollection<WorkspaceMember> Members { get; set; } = [];

    public ICollection<Conversation> Conversations { get; set; } = [];
}
