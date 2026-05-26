using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiniSlack.Application.Auth;
using MiniSlack.Endpoints;
using MiniSlack.Infrastructure;
using MiniSlack.Infrastructure.Auth;

const string ExternalCookieScheme = "ExternalOidc";
const string GoogleOidcScheme = "GoogleOidc";
const string VueCorsPolicy = "VueFrontend";

var builder = WebApplication.CreateBuilder(args);
var authOptions = builder.Configuration
    .GetSection(AuthOptions.SectionName)
    .Get<AuthOptions>() ?? new AuthOptions();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(VueCorsPolicy, policy =>
    {
        policy.WithOrigins(authOptions.Frontend.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie(ExternalCookieScheme, options =>
    {
        options.Cookie.Name = "__Host-minislack_external";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    })
    .AddOpenIdConnect(GoogleOidcScheme, options =>
    {
        options.SignInScheme = ExternalCookieScheme;
        options.Authority = "https://accounts.google.com";
        options.ClientId = authOptions.Google.ClientId;
        options.ClientSecret = authOptions.Google.ClientSecret;
        options.CallbackPath = authOptions.Google.CallbackPath;
        options.ResponseType = "code";
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(VueCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/auth/login/google", () =>
    Results.Challenge(
        new AuthenticationProperties
        {
            RedirectUri = "/auth/signed-in/google"
        },
        [GoogleOidcScheme]));

app.MapGet("/auth/signed-in/google", async (
    HttpContext httpContext,
    IAuthService authService,
    IOptions<AuthOptions> options,
    CancellationToken cancellationToken) =>
{
    var externalAuth = await httpContext.AuthenticateAsync(ExternalCookieScheme);
    if (!externalAuth.Succeeded || externalAuth.Principal is null)
    {
        return Results.Unauthorized();
    }

    var externalUser = CreateExternalUserInfo("google", externalAuth.Principal);
    if (externalUser is null)
    {
        await httpContext.SignOutAsync(ExternalCookieScheme);
        return Results.BadRequest(new { message = "Google did not return the required OIDC claims." });
    }

    var authResult = await authService.SignInExternalAsync(externalUser, cancellationToken);
    SetRefreshTokenCookie(httpContext, options.Value, authResult.RefreshToken);
    await httpContext.SignOutAsync(ExternalCookieScheme);

    return Results.Redirect(BuildFrontendRedirectUrl(options.Value));
});

app.MapPost("/auth/refresh", async (
    HttpContext httpContext,
    IAuthService authService,
    IOptions<AuthOptions> options,
    CancellationToken cancellationToken) =>
{
    var cookieName = options.Value.RefreshTokens.CookieName;
    if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var refreshToken))
    {
        return Results.Unauthorized();
    }

    var authResult = await authService.RefreshAsync(refreshToken, cancellationToken);
    if (authResult is null)
    {
        ClearRefreshTokenCookie(httpContext, options.Value);
        return Results.Unauthorized();
    }

    SetRefreshTokenCookie(httpContext, options.Value, authResult.RefreshToken);

    return Results.Ok(new
    {
        accessToken = authResult.AccessToken,
        expiresInSeconds = authResult.ExpiresInSeconds
    });
});

app.MapPost("/auth/logout", async (
    HttpContext httpContext,
    IAuthService authService,
    IOptions<AuthOptions> options,
    CancellationToken cancellationToken) =>
{
    var cookieName = options.Value.RefreshTokens.CookieName;
    if (httpContext.Request.Cookies.TryGetValue(cookieName, out var refreshToken))
    {
        await authService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
    }

    ClearRefreshTokenCookie(httpContext, options.Value);
    return Results.NoContent();
});

app.MapGet("/auth/me", async (
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var profile = await authService.GetCurrentUserAsync(user, cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    })
    .RequireAuthorization();

app.MapWorkspaceEndpoints();

app.MapGet("/weatherforecast", () =>
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering",
            "Scorching"
        };

        return Enumerable.Range(1, 5)
            .Select(index => new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]))
            .ToArray();
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi()
    .RequireAuthorization();

app.Run();

static ExternalUserInfo? CreateExternalUserInfo(string provider, ClaimsPrincipal principal)
{
    var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue("sub");
    var email = principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue("email");

    if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
    {
        return null;
    }

    var displayName = principal.FindFirstValue(ClaimTypes.Name)
        ?? principal.FindFirstValue("name")
        ?? email;
    var avatarUrl = principal.FindFirstValue("picture");
    var emailVerifiedClaim = principal.FindFirstValue("email_verified");
    var emailVerified = bool.TryParse(emailVerifiedClaim, out var parsedEmailVerified)
        && parsedEmailVerified;

    return new ExternalUserInfo(
        provider,
        externalId,
        email,
        emailVerified,
        displayName,
        avatarUrl);
}

static void SetRefreshTokenCookie(
    HttpContext httpContext,
    AuthOptions options,
    string refreshToken)
{
    httpContext.Response.Cookies.Append(
        options.RefreshTokens.CookieName,
        refreshToken,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(options.RefreshTokens.Days)
        });
}

static void ClearRefreshTokenCookie(
    HttpContext httpContext,
    AuthOptions options)
{
    httpContext.Response.Cookies.Delete(
        options.RefreshTokens.CookieName,
        new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
}

static string BuildFrontendRedirectUrl(AuthOptions options)
{
    var separator = options.Frontend.LoginCallbackUrl.Contains('?') ? "&" : "?";

    return $"{options.Frontend.LoginCallbackUrl}{separator}login=success";
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
