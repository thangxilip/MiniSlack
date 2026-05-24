namespace MiniSlack.Application.Auth;

public sealed record AuthResult(
    Guid UserId,
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken);
