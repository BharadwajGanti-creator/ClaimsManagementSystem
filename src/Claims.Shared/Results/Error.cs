namespace Claims.Shared.Results;

/// <summary>
/// Categorises a failure so the API layer can map it to the right HTTP status.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Unexpected
}

/// <summary>
/// A structured, transport-agnostic description of a failure.
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string message, string code = "validation_error") =>
        new(ErrorType.Validation, code, message);

    public static Error NotFound(string message, string code = "not_found") =>
        new(ErrorType.NotFound, code, message);

    public static Error Conflict(string message, string code = "conflict") =>
        new(ErrorType.Conflict, code, message);

    public static Error Unauthorized(string message, string code = "unauthorized") =>
        new(ErrorType.Unauthorized, code, message);

    public static Error Forbidden(string message, string code = "forbidden") =>
        new(ErrorType.Forbidden, code, message);
}
