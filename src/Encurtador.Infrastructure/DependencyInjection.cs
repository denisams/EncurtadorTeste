using Encurtador.Application.Abstractions;
using Encurtador.Application.Services;
using Encurtador.Infrastructure.BackgroundJobs;
using Encurtador.Infrastructure.Caching;
using Encurtador.Infrastructure.CodeGeneration;
using Encurtador.Infrastructure.Persistence;
using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Encurtador.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var mySqlConnectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Connection string 'MySql' is not configured.");
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        // A fixed ServerVersion (rather than ServerVersion.AutoDetect) avoids an extra
        // round-trip to the database on every startup and lets migrations be generated
        // at design time without a live database connection.
        var mySqlServerVersion = new MySqlServerVersion(new Version(8, 4, 0));

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(mySqlConnectionString, mySqlServerVersion));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddHangfire(config => config
            .UseStorage(new MySqlStorage(mySqlConnectionString, new MySqlStorageOptions { PrepareSchemaIfNecessary = true })));
        services.AddHangfireServer();

        services.AddScoped<IUrlRepository, UrlRepository>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IShortCodeGenerator, RedisShortCodeGenerator>();
        services.AddScoped<IClickTracker, HangfireClickTracker>();
        services.AddScoped<ClickTrackingJob>();
        services.AddScoped<UrlShortenerService>();

        return services;
    }
}
