namespace Claims.Application.Abstractions;

/// <summary>
/// Abstraction over where claim document binaries live (local disk in dev,
/// blob storage in production). Returns a storage key persisted on the document.
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
