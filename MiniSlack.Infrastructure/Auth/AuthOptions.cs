namespace MiniSlack.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Authentication";

    public JwtOptions Jwt { get; set; } = new();

    public RefreshTokenOptions RefreshTokens { get; set; } = new();

    public GoogleOidcOptions Google { get; set; } = new();

    public FrontendOptions Frontend { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "MiniSlack";

    public string Audience { get; set; } = "MiniSlack.Api";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}

public sealed class RefreshTokenOptions
{
    public int Days { get; set; } = 30;

    public string CookieName { get; set; } = "__Host-minislack_refresh";
}

public sealed class GoogleOidcOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackPath { get; set; } = "/auth/callback/google";
}

public sealed class FrontendOptions
{
    public string LoginCallbackUrl { get; set; } = "http://localhost:5173/auth/callback";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}
