using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using RiftLens.Contracts;
using RiftLens.Infrastructure.Local;
using RiftLens.Infrastructure.Riot;

namespace RiftLens.Api.Endpoints;

public static partial class MetaEndpoints
{
    public static IEndpointRouteBuilder MapMetaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/meta", (IOptions<RiotApiOptions> riot, IOptions<LocalModeOptions> local, IRiotKeyStore keys) =>
            TypedResults.Ok(new ServerInfoDto(
                typeof(MetaEndpoints).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
                riot.Value.DefaultPlatform.ToString(),
                DataDragonVersion: null,
                keys.State.ToString(),
                local.Value.Enabled)))
            .WithName("GetMeta");

        // Enregistrement de la clé Riot : uniquement depuis le poste lui-même (assistant du Desktop).
        app.MapPut("/api/admin/riot-key", SetRiotKeyAsync).WithName("SetRiotKey");
        return app;
    }

    internal static async Task<Results<NoContent, ProblemHttpResult>> SetRiotKeyAsync(
        RiotKeyRequest request, HttpContext http, IRiotKeyStore keys, IRiotApiClient riot, IOptions<RiotApiOptions> options, CancellationToken ct)
    {
        if (http.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "La clé Riot ne peut être modifiée que depuis ce poste.");
        }

        var key = request.Key?.Trim() ?? string.Empty;
        if (!KeyFormat().IsMatch(key))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Format attendu : RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.");
        }

        if (!await riot.ValidateKeyAsync(options.Value.DefaultPlatform, key, ct))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Riot refuse cette clé (invalide ou expirée).");
        }

        await keys.SetKeyAsync(key, ct);
        return TypedResults.NoContent();
    }

    [GeneratedRegex("^RGAPI-[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex KeyFormat();
}
