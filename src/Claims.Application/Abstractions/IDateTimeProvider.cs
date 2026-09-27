namespace Claims.Application.Abstractions;

/// <summary>
/// Abstracts the system clock so time-dependent logic can be tested deterministically.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
