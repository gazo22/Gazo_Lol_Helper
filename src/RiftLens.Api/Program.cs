using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RiftLens.Api.Endpoints;
using RiftLens.Api.Security;
using RiftLens.Infrastructure;
using RiftLens.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddRiftLensInfrastructure(builder.Configuration);
builder.Services.AddOptions<AccessKeyOptions>()
    .Bind(builder.Configuration.GetSection(AccessKeyOptions.SectionName))
    .Validate(o => o.Keys.Exists(k => k.Length >= 32), "Access:Keys doit contenir au moins une clé de 32 caractères minimum.")
    .ValidateOnStart();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<RiftLensDbContext>("database");

// Limite entrante par IP : protège le quota Riot si l'Api est exposée.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseMiddleware<AccessKeyMiddleware>();

app.MapHealthChecks("/health");
app.MapMetaEndpoints();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<RiftLensDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

/// <summary>Point d'entrée exposé pour <c>WebApplicationFactory</c> dans les tests.</summary>
public partial class Program;
