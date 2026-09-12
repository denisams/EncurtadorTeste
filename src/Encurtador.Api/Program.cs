using Encurtador.Api.Endpoints;
using Encurtador.Infrastructure;
using Encurtador.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    // Redirects are the overwhelming majority of traffic for a URL shortener,
    // so they get a much higher ceiling than the write path.
    options.AddFixedWindowLimiter("redirect", limiterOptions =>
    {
        limiterOptions.PermitLimit = 200;
        limiterOptions.Window = TimeSpan.FromSeconds(1);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("shorten", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromSeconds(1);
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRateLimiter();

app.MapUrlEndpoints();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

public partial class Program;
