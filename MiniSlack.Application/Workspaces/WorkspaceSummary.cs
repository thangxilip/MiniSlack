using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Application.Workspaces;

public sealed record WorkspaceSummary(
    Guid Id,
    string Name,
    string Slug,
    WorkspaceMemberRole Role);
