namespace Encurtador.Application.Abstractions;

/// <summary>
/// Records a click off the redirect hot path (fire-and-forget via a background job queue),
/// so persisting analytics never adds latency to a redirect request.
/// </summary>
public interface IClickTracker
{
    void TrackClick(string code);
}
