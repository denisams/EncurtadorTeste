using Encurtador.Application.Abstractions;
using Encurtador.Application.Services;
using Encurtador.Infrastructure.BackgroundJobs;
using Encurtador.Infrastructure.Caching;
using Encurtador.Infrastructure.CodeGeneration;
using Encurtador.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Encurtador.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgresConnectionString));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddHangfire(config => config
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(postgresConnectionString)));
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
