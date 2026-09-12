namespace Encurtador.Application.Abstractions;

/// <summary>
/// Read-through/cache-aside abstraction over the hot key-value store (Redis in production).
/// Kept generic so the Application layer never depends on StackExchange.Redis directly.
/// </summary>
public interface ICacheService
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
