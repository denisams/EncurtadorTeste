namespace Encurtador.Application.Dtos;

public record ShortenUrlRequest(string OriginalUrl, string? CustomAlias = null, TimeSpan? TimeToLive = null);
