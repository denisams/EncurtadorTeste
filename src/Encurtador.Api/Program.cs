using Encurtador.Api.Endpoints;
using Encurtador.Infrastructure;
using Encurtador.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EncurtadorTeste API",
        Version = "v1",
        Description = "Encurtador de URLs preparado para alto volume de redirecionamentos.",
    });
});

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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EncurtadorTeste API v1");
        options.RoutePrefix = "swagger";
    });

    app.UseHangfireDashboard("/hangfire");

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

public partial class Program;
