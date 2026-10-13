namespace RiftLens.UI.Services;

public enum ApiError
{
    None,
    NotFound,
    Unauthorized,
    RiotKeyInvalid,
    RiotKeyMissing,
    RateLimited,
    Offline,
    Server,
}

/// <summary>Résultat d'un appel au backend : valeur ou erreur typée pour afficher l'état adéquat.</summary>
public sealed record ApiResult<T>(T? Value, ApiError Error, string? Message = null)
{
    public bool IsSuccess => Error == ApiError.None && Value is not null;
}

public static class ApiResult
{
    public static ApiResult<T> Ok<T>(T value) => new(value, ApiError.None);

    public static ApiResult<T> Fail<T>(ApiError error, string? message = null) => new(default, error, message);
}
