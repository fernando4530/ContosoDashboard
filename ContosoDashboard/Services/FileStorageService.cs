using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class FileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly string _temporaryRoot;
    private const long MaxBytes = 25 * 1024 * 1024;

    public FileStorageService(IOptions<DocumentStorageOptions> options, IWebHostEnvironment environment)
    {
        var value = options.Value;
        _root = Resolve(value.RootPath, environment.ContentRootPath);
        _temporaryRoot = Resolve(value.TemporaryPath, environment.ContentRootPath);
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_temporaryRoot);
    }

    public async Task<string> WriteTemporaryAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        var key = $".temporary/{Guid.NewGuid():N}{NormalizeExtension(extension)}";
        var path = ResolveTemporaryKey(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await CopyLimitedAsync(content, path, cancellationToken);
        return key;
    }

    public async Task PromoteAsync(string temporaryKey, string storageKey, CancellationToken cancellationToken)
    {
        var source = ResolveTemporaryKey(temporaryKey);
        var destination = ResolveKey(storageKey);
        if (!File.Exists(source)) throw new FileNotFoundException("Temporary document was not found.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await Task.Run(() => File.Move(source, destination, true), cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = ResolveKey(storageKey);
        if (!File.Exists(path)) throw new FileNotFoundException("Document was not found.");
        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = ResolveKey(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<int> CleanupTemporaryAsync(CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(_temporaryRoot, "*", SearchOption.AllDirectories))
        {
            File.Delete(file);
            count++;
        }
        return Task.FromResult(count);
    }

    private string ResolveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Storage keys must be relative and cannot traverse directories.", nameof(key));
        var normalized = key.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(_root, normalized));
        var root = EnsureTrailingSeparator(Path.GetFullPath(_root));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Storage key is outside the storage root.", nameof(key));
        return path;
    }

    private string ResolveTemporaryKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Temporary keys must be relative and cannot traverse directories.", nameof(key));
        var relative = key.Replace('/', Path.DirectorySeparatorChar).Replace(".temporary" + Path.DirectorySeparatorChar, string.Empty, StringComparison.OrdinalIgnoreCase);
        var path = Path.GetFullPath(Path.Combine(_temporaryRoot, relative));
        var root = EnsureTrailingSeparator(Path.GetFullPath(_temporaryRoot));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Temporary key is outside the temporary root.", nameof(key));
        return path;
    }

    private static async Task CopyLimitedAsync(Stream content, string path, CancellationToken cancellationToken)
    {
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
        var buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > MaxBytes) throw new InvalidDataException("The document exceeds the 25 MiB limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static string Resolve(string configured, string root) => Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(root, configured));
    private static string EnsureTrailingSeparator(string path) => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    private static string NormalizeExtension(string extension) => string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
}