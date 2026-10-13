using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RiftLens.Api.Endpoints;
using RiftLens.Api.Security;
using Microsoft.Extensions.Options;
using RiftLens.Infrastructure;
using RiftLens.Infrastructure.Collection;
using RiftLens.Infrastructure.Local;
using RiftLens.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Service Windows : le dossier courant est System32, on se base sur celui de l'exécutable.
    ContentRootPath = AppContext.BaseDirectory,
});

// Mode tout-en-un (service Windows installé par le setup) : réglages locaux, puis env / ligne de commande prioritaires.
var localMode = builder.Configuration.GetValue<bool>("LocalMode:Enabled");
if (localMode)
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: false);
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);
    builder.Host.UseWindowsService(o => o.ServiceName = "RiftLens Server");
}

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
    if (localMode)
    {
        var dataDirectory = ctx.Configuration["LocalMode:DataDirectory"] ?? LocalModeOptions.DefaultDataDirectory;
        cfg.WriteTo.File(Path.Combine(dataDirectory, "logs", "server-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
    }
});

builder.Services.AddRiftLensInfrastructure(builder.Configuration);
builder.Services.AddOptions<AccessKeyOptions>()
    .Bind(builder.Configuration.GetSection(AccessKeyOptions.SectionName))
    .PostConfigure<IOptions<LocalModeOptions>>((o, local) =>
    {
        if (local.Value.Enabled && o.Keys.Count == 0)
        {
            o.Keys.Add(LocalAccessKey.EnsureCreated(local.Value.DataDirectory));
        }
    })
    .Validate(o => o.Keys.Exists(k => k.Length >= 32), "Access:Keys doit contenir au moins une clé de 32 caractères minimum.")
    .ValidateOnStart();

if (builder.Configuration.GetValue<bool>("Collector:Enabled"))
{
    // Collector dans le même processus : un seul rate limiter, l'Api (interactive) passe avant la collecte (fond).
    builder.Services.AddHostedService<CollectorWorker>();
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RiotExceptionHandler>();
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
app.MapPlayerEndpoints();
app.MapMatchEndpoints();
app.MapStatsEndpoints();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    // Au démarrage de Windows, SQL Server Express peut ne pas être prêt : on réessaie au lieu de faire échouer le service.
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<RiftLensDbContext>().Database.MigrateAsync();
            break;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (attempt < 12)
        {
            app.Logger.LogWarning(ex, "Base indisponible (tentative {Attempt}/12), nouvel essai dans 10 s", attempt);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}

await app.RunAsync();

/// <summary>Point d'entrée exposé pour <c>WebApplicationFactory</c> dans les tests.</summary>
public partial class Program;
