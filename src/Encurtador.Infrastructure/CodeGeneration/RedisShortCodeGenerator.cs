using Encurtador.Application.Abstractions;
using Encurtador.Domain.Common;
using StackExchange.Redis;

namespace Encurtador.Infrastructure.CodeGeneration;

/// <summary>
/// Generates unique short codes from a single Redis INCR counter. Redis processes
/// commands on one thread, so INCR is atomic and collision-free without any locking,
/// and it comfortably sustains the throughput a redirect service needs for writes
/// (which are a small fraction of total traffic compared to redirects).
///
/// For extreme write volume beyond a single Redis instance, this can be sharded by
/// running N counters ("url:code:counter:0".."N-1") and having each API instance
/// pinned to one shard, or by switching to a Snowflake-style worker-id + timestamp
/// scheme so no shared counter is needed at all.
/// </summary>
public class RedisShortCodeGenerator : IShortCodeGenerator
{
    private const string CounterKey = "url:code:counter";

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisShortCodeGenerator(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string> NextCodeAsync(CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var next = await database.StringIncrementAsync(CounterKey);
        return Base62.Encode(next);
    }
}
