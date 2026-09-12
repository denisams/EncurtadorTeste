using Encurtador.Application.Abstractions;
using Encurtador.Application.Dtos;
using Encurtador.Application.Services;
using Encurtador.Domain.Entities;
using Moq;

namespace Encurtador.Tests.Services;

public class UrlShortenerServiceTests
{
    private readonly Mock<IUrlRepository> _repository = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IShortCodeGenerator> _codeGenerator = new();
    private readonly Mock<IClickTracker> _clickTracker = new();
    private readonly UrlShortenerService _sut;

    public UrlShortenerServiceTests()
    {
        _sut = new UrlShortenerService(_repository.Object, _cache.Object, _codeGenerator.Object, _clickTracker.Object);
    }

    [Fact]
    public async Task ShortenAsync_WithValidUrl_GeneratesCodeAndCachesResult()
    {
        _codeGenerator.Setup(g => g.NextCodeAsync(It.IsAny<CancellationToken>())).ReturnsAsync("abc123");

        var result = await _sut.ShortenAsync(new ShortenUrlRequest("https://example.com/page"));

        Assert.Equal(ShortenUrlOutcome.Created, result.Outcome);
        Assert.Equal("abc123", result.Response!.Code);
        _repository.Verify(r => r.AddAsync(It.Is<ShortenedUrl>(u => u.Code == "abc123"), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync("url:abc123", "https://example.com/page", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShortenAsync_WithInvalidUrl_ReturnsInvalidUrlOutcome()
    {
        var result = await _sut.ShortenAsync(new ShortenUrlRequest("not-a-url"));

        Assert.Equal(ShortenUrlOutcome.InvalidUrl, result.Outcome);
        _repository.Verify(r => r.AddAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShortenAsync_WithAliasAlreadyTaken_ReturnsConflictOutcome()
    {
        _repository.Setup(r => r.ExistsAsync("meu-link", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.ShortenAsync(new ShortenUrlRequest("https://example.com", CustomAlias: "meu-link"));

        Assert.Equal(ShortenUrlOutcome.AliasAlreadyInUse, result.Outcome);
    }

    [Fact]
    public async Task ResolveAsync_WhenCacheHit_ReturnsCachedUrlWithoutHittingRepository()
    {
        _cache.Setup(c => c.GetAsync("url:abc123", It.IsAny<CancellationToken>())).ReturnsAsync("https://example.com/page");

        var result = await _sut.ResolveAsync("abc123");

        Assert.Equal("https://example.com/page", result);
        _repository.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _clickTracker.Verify(t => t.TrackClick("abc123"), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenCacheMissAndFoundInRepository_PopulatesCache()
    {
        _cache.Setup(c => c.GetAsync("url:abc123", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _repository.Setup(r => r.GetByCodeAsync("abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShortenedUrl { Code = "abc123", OriginalUrl = "https://example.com/page", CreatedAtUtc = DateTime.UtcNow });

        var result = await _sut.ResolveAsync("abc123");

        Assert.Equal("https://example.com/page", result);
        _cache.Verify(c => c.SetAsync("url:abc123", "https://example.com/page", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenExpired_ReturnsNull()
    {
        _cache.Setup(c => c.GetAsync("url:abc123", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _repository.Setup(r => r.GetByCodeAsync("abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShortenedUrl
            {
                Code = "abc123",
                OriginalUrl = "https://example.com/page",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                ExpiresAtUtc = DateTime.UtcNow.AddHours(-1),
            });

        var result = await _sut.ResolveAsync("abc123");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_WhenNotFound_ReturnsNull()
    {
        _cache.Setup(c => c.GetAsync("url:missing", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _repository.Setup(r => r.GetByCodeAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((ShortenedUrl?)null);

        var result = await _sut.ResolveAsync("missing");

        Assert.Null(result);
        _clickTracker.Verify(t => t.TrackClick(It.IsAny<string>()), Times.Never);
    }
}
