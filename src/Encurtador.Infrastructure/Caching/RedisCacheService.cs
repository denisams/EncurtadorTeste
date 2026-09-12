using Encurtador.Application.Abstractions;
using StackExchange.Redis;

namespace Encurtador.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public Task SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) =>
        Database.StringSetAsync(key, value, expiry: ttl.HasValue ? (Expiration)ttl.Value : Expiration.Persist);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(key);
}
