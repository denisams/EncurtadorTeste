using Encurtador.Domain.Entities;

namespace Encurtador.Application.Abstractions;

public interface IUrlRepository
{
    Task AddAsync(ShortenedUrl url, CancellationToken cancellationToken = default);

    Task<ShortenedUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);

    Task IncrementClickCountAsync(string code, CancellationToken cancellationToken = default);
}
