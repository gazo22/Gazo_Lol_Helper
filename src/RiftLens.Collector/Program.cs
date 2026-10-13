using RiftLens.Infrastructure;
using RiftLens.Infrastructure.Collection;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((_, cfg) => cfg
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddRiftLensInfrastructure(builder.Configuration);
builder.Services.AddHostedService<CollectorWorker>();

await builder.Build().RunAsync();
