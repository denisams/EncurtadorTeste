using Encurtador.Application.Abstractions;
using Encurtador.Application.Dtos;
using Encurtador.Domain.Entities;

namespace Encurtador.Application.Services;

public class UrlShortenerService
{
    // Redirects vastly outnumber creations for a URL shortener, so cached entries
    // are kept warm for a while to keep the read path off the database almost entirely.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private const string CacheKeyPrefix = "url:";

    private readonly IUrlRepository _repository;
    private readonly ICacheService _cache;
    private readonly IShortCodeGenerator _codeGenerator;
    private readonly IClickTracker _clickTracker;

    public UrlShortenerService(
        IUrlRepository repository,
        ICacheService cache,
        IShortCodeGenerator codeGenerator,
        IClickTracker clickTracker)
    {
        _repository = repository;
        _cache = cache;
        _codeGenerator = codeGenerator;
        _clickTracker = clickTracker;
    }

    public async Task<ShortenUrlResult> ShortenAsync(ShortenUrlRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new ShortenUrlResult(ShortenUrlOutcome.InvalidUrl);
        }

        string code;
        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            code = request.CustomAlias.Trim();
            if (await _repository.ExistsAsync(code, cancellationToken))
            {
                return new ShortenUrlResult(ShortenUrlOutcome.AliasAlreadyInUse);
            }
        }
        else
        {
            code = await _codeGenerator.NextCodeAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        var entity = new ShortenedUrl
        {
            Code = code,
            OriginalUrl = uri.ToString(),
            CreatedAtUtc = now,
            ExpiresAtUtc = request.TimeToLive.HasValue ? now.Add(request.TimeToLive.Value) : null,
        };

        await _repository.AddAsync(entity, cancellationToken);
        await _cache.SetAsync(CacheKeyPrefix + code, entity.OriginalUrl, CacheTtl, cancellationToken);

        var response = new ShortenUrlResponse(entity.Code, entity.OriginalUrl, entity.CreatedAtUtc, entity.ExpiresAtUtc);
        return new ShortenUrlResult(ShortenUrlOutcome.Created, response);
    }

    public async Task<string?> ResolveAsync(string code, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + code;

        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _clickTracker.TrackClick(code);
            return cached;
        }

        var entity = await _repository.GetByCodeAsync(code, cancellationToken);
        if (entity is null || (entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value <= DateTime.UtcNow))
        {
            return null;
        }

        await _cache.SetAsync(cacheKey, entity.OriginalUrl, CacheTtl, cancellationToken);
        _clickTracker.TrackClick(code);

        return entity.OriginalUrl;
    }
}
