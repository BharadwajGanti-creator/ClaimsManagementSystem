using Claims.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Claims.Infrastructure.Storage;

/// <summary>
/// Stores claim documents on the local file system. In production this would be
/// swapped for an Azure Blob Storage implementation behind the same interface.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:LocalRootPath"]
                    ?? Path.Combine(AppContext.BaseDirectory, "claim-documents");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, key);

        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);

        return key; // Relative key; resolved against _rootPath on read.
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
