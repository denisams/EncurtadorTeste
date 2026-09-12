namespace Encurtador.Application.Dtos;

public record ShortenUrlResponse(string Code, string OriginalUrl, DateTime CreatedAtUtc, DateTime? ExpiresAtUtc);
