using System.Security.Claims;

namespace MiniSlack.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> SignInExternalAsync(
        ExternalUserInfo externalUser,
        CancellationToken cancellationToken = default);

    Task<AuthResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
