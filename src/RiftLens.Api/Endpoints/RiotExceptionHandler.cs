using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using RiftLens.Infrastructure.Players;
using RiftLens.Infrastructure.Riot;

namespace RiftLens.Api.Endpoints;

/// <summary>Traduit les erreurs Riot / métier en ProblemDetails explicites pour le Desktop.</summary>
internal sealed partial class RiotExceptionHandler(IProblemDetailsService problems, ILogger<RiotExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Code renvoyé au client quand la clé Riot du serveur est expirée.</summary>
    public const string RiotKeyExpiredCode = "riot-key-invalid";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title, string? code) = exception switch
        {
            PlayerNotFoundException e => (StatusCodes.Status404NotFound, e.Message, "player-not-found"),
            RiotApiAuthException => (StatusCodes.Status503ServiceUnavailable, "La clé API Riot du serveur est invalide ou expirée.", RiotKeyExpiredCode),
            RiotApiException { StatusCode: HttpStatusCode.TooManyRequests } => (StatusCodes.Status429TooManyRequests, "Quota Riot atteint, réessayez dans un instant.", "riot-rate-limited"),
            RiotApiException e => (StatusCodes.Status502BadGateway, $"L'API Riot a répondu une erreur ({(int?)e.StatusCode}).", "riot-error"),
            HttpRequestException => (StatusCodes.Status502BadGateway, "API Riot injoignable.", "riot-unreachable"),
            _ => (0, string.Empty, null),
        };

        if (status == 0)
        {
            return false;
        }

        LogHandled(logger, exception, status, code);
        httpContext.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Type = code, Extensions = { ["code"] = code } },
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Erreur traduite en {Status} ({Code})")]
    private static partial void LogHandled(ILogger logger, Exception ex, int status, string? code);
}
