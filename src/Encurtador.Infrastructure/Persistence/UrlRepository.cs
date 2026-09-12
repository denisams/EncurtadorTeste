using Encurtador.Application.Abstractions;
using Encurtador.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Infrastructure.Persistence;

public class UrlRepository : IUrlRepository
{
    private readonly AppDbContext _dbContext;

    public UrlRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ShortenedUrl url, CancellationToken cancellationToken = default)
    {
        _dbContext.ShortenedUrls.Add(url);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ShortenedUrl?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.ShortenedUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Code == code, cancellationToken);

    public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.ShortenedUrls.AsNoTracking().AnyAsync(u => u.Code == code, cancellationToken);

    public async Task IncrementClickCountAsync(string code, CancellationToken cancellationToken = default)
    {
        // Runs off the redirect hot path (triggered from a Hangfire job), so a
        // straightforward atomic UPDATE is preferred over loading + saving the entity.
        await _dbContext.ShortenedUrls
            .Where(u => u.Code == code)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.ClickCount, u => u.ClickCount + 1), cancellationToken);
    }
}
