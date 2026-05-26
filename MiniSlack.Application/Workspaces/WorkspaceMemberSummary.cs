using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Application.Workspaces;

public sealed record WorkspaceMemberSummary(
    Guid UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    string? Status,
    WorkspaceMemberRole Role,
    DateTimeOffset JoinedAtUtc);
