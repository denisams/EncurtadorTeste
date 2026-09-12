using Encurtador.Application.Dtos;
using Encurtador.Application.Services;

namespace Encurtador.Api.Endpoints;

public static class UrlEndpoints
{
    public static IEndpointRouteBuilder MapUrlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/urls", async (ShortenUrlRequest request, UrlShortenerService service, HttpRequest httpRequest, CancellationToken cancellationToken) =>
        {
            var result = await service.ShortenAsync(request, cancellationToken);

            return result.Outcome switch
            {
                ShortenUrlOutcome.Created => Results.Created(
                    $"{httpRequest.Scheme}://{httpRequest.Host}/{result.Response!.Code}",
                    result.Response),
                ShortenUrlOutcome.AliasAlreadyInUse => Results.Conflict(new { message = "O alias informado já está em uso." }),
                ShortenUrlOutcome.InvalidUrl => Results.BadRequest(new { message = "A URL informada é inválida." }),
                _ => Results.Problem(),
            };
        })
        .WithName("ShortenUrl")
        .WithSummary("Encurta uma URL")
        .WithDescription("Gera um código curto para a URL informada. Aceita um alias customizado opcional e um tempo de expiração opcional.")
        .Produces<ShortenUrlResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .RequireRateLimiting("shorten");

        app.MapGet("/{code}", async (string code, UrlShortenerService service, CancellationToken cancellationToken) =>
        {
            var originalUrl = await service.ResolveAsync(code, cancellationToken);
            return originalUrl is not null
                ? Results.Redirect(originalUrl, permanent: false)
                : Results.NotFound();
        })
        .WithName("ResolveUrl")
        .WithSummary("Redireciona para a URL original")
        .WithDescription("Resolve um código curto e redireciona (302) para a URL original. Retorna 404 se o código não existir ou tiver expirado.")
        .Produces(StatusCodes.Status302Found)
        .Produces(StatusCodes.Status404NotFound)
        .RequireRateLimiting("redirect");

        return app;
    }
}
