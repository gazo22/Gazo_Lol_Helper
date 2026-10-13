using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RiftLens.Contracts;

namespace RiftLens.Api.Security;

/// <summary>
/// Protège toutes les routes <c>/api</c> par une clé partagée, comparée en temps constant.
/// Les routes de santé restent publiques.
/// </summary>
public sealed partial class AccessKeyMiddleware(RequestDelegate next, IOptionsMonitor<AccessKeyOptions> options, ILogger<AccessKeyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var provided = context.Request.Headers[ApiConventions.AccessKeyHeader].ToString();
        if (provided.Length == 0 || !IsValid(provided, options.CurrentValue.Keys))
        {
            LogRejected(logger, context.Connection.RemoteIpAddress?.ToString(), context.Request.Path);
            await Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Clé d'accès invalide ou absente.")
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    internal static bool IsValid(string provided, IEnumerable<string> keys)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var match = false;
        foreach (var key in keys.Where(k => !string.IsNullOrEmpty(k)))
        {
            // Pas de court-circuit : on parcourt toutes les clés.
            match |= CryptographicOperations.FixedTimeEquals(providedBytes, Encoding.UTF8.GetBytes(key));
        }

        return match;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Requête refusée (clé d'accès) depuis {RemoteIp} sur {Path}")]
    private static partial void LogRejected(ILogger logger, string? remoteIp, PathString path);
}
