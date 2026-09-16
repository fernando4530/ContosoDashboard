using Microsoft.AspNetCore.Components.Forms;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> WriteTemporaryAsync(Stream content, string extension, CancellationToken cancellationToken);
    Task PromoteAsync(string temporaryKey, string storageKey, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public enum MalwareScanStatus { Available, Rejected, Unavailable }

public sealed record MalwareScanResult(MalwareScanStatus Status, string? Reason = null);

public interface IMalwareScanner
{
    Task<MalwareScanResult> ScanAsync(Stream content, CancellationToken cancellationToken);
}

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
    public string TemporaryPath { get; set; } = "AppData/uploads/.temporary";
    public string ScannerMode { get; set; } = DocumentConstants.AvailableScanner;
}

public sealed class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
}

public sealed record DocumentUploadResult(string FileName, bool Success, string Message, Document? Document = null);

public sealed class DocumentQuery
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string SortBy { get; set; } = "date";
    public bool Descending { get; set; } = true;
}