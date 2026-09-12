namespace Encurtador.Application.Abstractions;

/// <summary>
/// Produces unique short codes without contending on the primary database,
/// so code generation stays fast and horizontally scalable across API instances.
/// </summary>
public interface IShortCodeGenerator
{
    Task<string> NextCodeAsync(CancellationToken cancellationToken = default);
}
