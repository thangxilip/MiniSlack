using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniSlack.Application.Auth;
using MiniSlack.Domain.Auth;
using MiniSlack.Domain.Users;
using MiniSlack.Infrastructure.Persistence;

namespace MiniSlack.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public AuthService(
        AppDbContext dbContext,
        ITokenService tokenService,
        IOptions<AuthOptions> options)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _refreshTokenOptions = options.Value.RefreshTokens;
    }

    public async Task<AuthResult> SignInExternalAsync(
        ExternalUserInfo externalUser,
        CancellationToken cancellationToken = default)
    {
        if (!externalUser.EmailVerified)
        {
            throw new InvalidOperationException("The external provider did not verify this email address.");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.IdentityProvider == externalUser.IdentityProvider
                && candidate.ExternalId == externalUser.ExternalId,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                IdentityProvider = externalUser.IdentityProvider,
                ExternalId = externalUser.ExternalId,
                Email = externalUser.Email,
                EmailVerified = externalUser.EmailVerified,
                DisplayName = externalUser.DisplayName,
                AvatarUrl = externalUser.AvatarUrl,
                LastLoginAtUtc = DateTimeOffset.UtcNow
            };

            _dbContext.Users.Add(user);
        }
        else
        {
            user.Email = externalUser.Email;
            user.EmailVerified = externalUser.EmailVerified;
            user.DisplayName = externalUser.DisplayName;
            user.AvatarUrl = externalUser.AvatarUrl;
            user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken?.User is null
            || storedToken.RevokedAtUtc is not null
            || storedToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;

        return await IssueTokensAsync(storedToken.User, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is not null && storedToken.RevokedAtUtc is null)
        {
            storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<UserProfile?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => new UserProfile(
                user.Id,
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<AuthResult> IssueTokensAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var refreshToken = GenerateRefreshToken();
        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_refreshTokenOptions.Days)
        };

        _dbContext.RefreshTokens.Add(storedRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            user.Id,
            _tokenService.CreateAccessToken(user),
            _tokenService.AccessTokenLifetimeSeconds,
            refreshToken);
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
