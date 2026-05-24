using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiniSlack.Application.Auth;
using MiniSlack.Domain.Users;

namespace MiniSlack.Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value.Jwt;
    }

    public int AccessTokenLifetimeSeconds => _options.AccessTokenMinutes * 60;

    public string CreateAccessToken(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            claims.Add(new Claim("picture", user.AvatarUrl));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(_options.AccessTokenMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
