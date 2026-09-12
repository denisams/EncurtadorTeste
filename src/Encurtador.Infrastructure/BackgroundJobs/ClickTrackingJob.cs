using Encurtador.Application.Abstractions;

namespace Encurtador.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs on the Hangfire worker, decoupled from the redirect request. Keeping the
/// click-count write here means a slow or momentarily unavailable database never
/// adds latency to (or fails) the redirect itself.
/// </summary>
public class ClickTrackingJob
{
    private readonly IUrlRepository _repository;

    public ClickTrackingJob(IUrlRepository repository)
    {
        _repository = repository;
    }

    public Task IncrementAsync(string code) => _repository.IncrementClickCountAsync(code);
}
