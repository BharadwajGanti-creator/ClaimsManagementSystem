namespace Claims.Domain.Exceptions;

/// <summary>
/// Raised when a domain invariant or business rule is violated. The API layer
/// translates this into an HTTP 409/422 response.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
