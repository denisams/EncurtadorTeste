using Encurtador.Application.Abstractions;
using Hangfire;

namespace Encurtador.Infrastructure.BackgroundJobs;

public class HangfireClickTracker : IClickTracker
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireClickTracker(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void TrackClick(string code) =>
        _backgroundJobClient.Enqueue<ClickTrackingJob>(job => job.IncrementAsync(code));
}
