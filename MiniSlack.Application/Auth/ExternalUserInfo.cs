namespace MiniSlack.Application.Auth;

public sealed record ExternalUserInfo(
    string IdentityProvider,
    string ExternalId,
    string Email,
    bool EmailVerified,
    string DisplayName,
    string? AvatarUrl);
