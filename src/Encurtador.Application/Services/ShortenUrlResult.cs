using Encurtador.Application.Dtos;

namespace Encurtador.Application.Services;

public enum ShortenUrlOutcome
{
    Created,
    AliasAlreadyInUse,
    InvalidUrl,
}

public record ShortenUrlResult(ShortenUrlOutcome Outcome, ShortenUrlResponse? Response = null);
