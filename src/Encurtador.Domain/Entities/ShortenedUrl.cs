namespace Encurtador.Domain.Entities;

public class ShortenedUrl
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string OriginalUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public long ClickCount { get; set; }
}
