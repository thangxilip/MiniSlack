namespace MiniSlack.Application.Auth;

public sealed record UserProfile(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Status);
