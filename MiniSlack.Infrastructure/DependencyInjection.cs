using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniSlack.Application.Auth;
using MiniSlack.Application.Common.Persistence;
using MiniSlack.Application.Realtime;
using MiniSlack.Application.Workspaces.Abstractions;
using MiniSlack.Infrastructure.Auth;
using MiniSlack.Infrastructure.Persistence;
using MiniSlack.Infrastructure.Persistence.Repositories;
using MiniSlack.Infrastructure.Realtime;
using MiniSlack.Infrastructure.Workspaces.Stores;

namespace MiniSlack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadOnlyRepository<>), typeof(EfRepository<>));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkspaceCommandStore, WorkspaceCommandStore>();
        services.AddScoped<IWorkspaceReadStore, WorkspaceReadStore>();
        services.AddSingleton<IUserPresenceTracker, InMemoryUserPresenceTracker>();

        return services;
    }
}
