using MiniSlack.Domain.Users;

namespace MiniSlack.Application.Auth;

public interface ITokenService
{
    int AccessTokenLifetimeSeconds { get; }

    string CreateAccessToken(User user);
}
