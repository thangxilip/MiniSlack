using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MiniSlack.Domain.Configurations;
using MiniSlack.Infrastructure.Persistence;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

public class EponaDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{

    AppDbContext IDesignTimeDbContextFactory<AppDbContext>.CreateDbContext(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.IsNullOrEmpty(envName))
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var environment = envName ?? "Development";
        
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var appConfig = configuration.Get<AppConfig>() ?? throw new NullReferenceException("Invalid configuration");
        // var httpContextAccessor = new HttpContextAccessor();
        // var requestContext = new RequestContext(httpContextAccessor);
        // var options = Options.Create(appConfig);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(appConfig.ConnectionStrings.DefaultConnection);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        var npgsqlBuilderFunc = new Action<NpgsqlDbContextOptionsBuilder>(builder =>
            builder.MigrationsHistoryTable("__EFMigrationsHistory", "dev"));
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.
            UseNpgsql(dataSource, npgsqlBuilderFunc).
            UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}
