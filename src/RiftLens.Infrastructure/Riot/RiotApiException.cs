using System.Net;

namespace RiftLens.Infrastructure.Riot;

public class RiotApiException : Exception
{
    public RiotApiException() { }

    public RiotApiException(string message) : base(message) { }

    public RiotApiException(string message, Exception inner) : base(message, inner) { }

    public RiotApiException(HttpStatusCode statusCode, string message) : base(message) => StatusCode = statusCode;

    public HttpStatusCode? StatusCode { get; }
}

/// <summary>La clé Riot est absente, invalide ou expirée (401/403). Les clés de développement expirent toutes les 24 h.</summary>
public sealed class RiotApiAuthException : RiotApiException
{
    public RiotApiAuthException() { }

    public RiotApiAuthException(string message) : base(message) { }

    public RiotApiAuthException(string message, Exception inner) : base(message, inner) { }

    public RiotApiAuthException(HttpStatusCode statusCode, string message) : base(statusCode, message) { }
}
