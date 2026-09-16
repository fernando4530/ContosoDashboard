using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Infrastructure;

public sealed class FakeMalwareScanner : IMalwareScanner
{
    public MalwareScanResult Result { get; set; } = new(MalwareScanStatus.Available);
    public Task<MalwareScanResult> ScanAsync(Stream content, CancellationToken cancellationToken) => Task.FromResult(Result);
}

public sealed class FakeFileStorageService : IFileStorageService
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);
    public bool FailPromotion { get; set; }

    public async Task<string> WriteTemporaryAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var key = $".temporary/{Guid.NewGuid():N}{extension}";
        Files[key] = buffer.ToArray();
        return key;
    }

    public Task PromoteAsync(string temporaryKey, string storageKey, CancellationToken cancellationToken)
    {
        if (FailPromotion) throw new IOException("Configured promotion failure.");
        Files[storageKey] = Files[temporaryKey]; Files.Remove(temporaryKey);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Files[storageKey], writable: false));
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) { Files.Remove(storageKey); return Task.CompletedTask; }
}