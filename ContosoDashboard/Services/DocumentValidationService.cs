using Microsoft.AspNetCore.Components.Forms;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record DocumentValidationResult(bool IsValid, string? Error, string Extension, string ContentType);

public interface IDocumentValidationService
{
    Task<DocumentValidationResult> ValidateAsync(IBrowserFile file, DocumentUploadRequest request, CancellationToken cancellationToken = default);
}

public sealed class DocumentValidationService : IDocumentValidationService
{
    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { "application/pdf" },
        [".doc"] = new[] { "application/msword" },
        [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip" },
        [".xls"] = new[] { "application/vnd.ms-excel" },
        [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip" },
        [".ppt"] = new[] { "application/vnd.ms-powerpoint" },
        [".pptx"] = new[] { "application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/zip" },
        [".txt"] = new[] { "text/plain" },
        [".jpg"] = new[] { "image/jpeg" },
        [".jpeg"] = new[] { "image/jpeg" },
        [".png"] = new[] { "image/png" }
    };

    public async Task<DocumentValidationResult> ValidateAsync(IBrowserFile file, DocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return Invalid("A title is required.");
        if (!DocumentCategories.All.Contains(request.Category, StringComparer.Ordinal)) return Invalid("A valid category is required.");
        if (file.Size < 1 || file.Size > DocumentConstants.MaxFileSize) return Invalid("The file must be between 1 byte and 25 MiB.");
        var extension = Path.GetExtension(file.Name);
        if (!Allowed.TryGetValue(extension, out var mimeTypes)) return Invalid("The file format is not supported.");
        if (!mimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return Invalid("The file extension and MIME type do not match.");

        await using var stream = file.OpenReadStream(DocumentConstants.MaxFileSize, cancellationToken);
        var header = new byte[16];
        var read = await stream.ReadAsync(header, cancellationToken);
        if (!HasValidSignature(extension, header, read)) return Invalid("The file content does not match its declared format.");
        return new DocumentValidationResult(true, null, extension.ToLowerInvariant(), file.ContentType);
    }

    private static bool HasValidSignature(string extension, byte[] header, int read) => extension.ToLowerInvariant() switch
    {
        ".pdf" => read >= 5 && header[0] == '%' && header[1] == 'P' && header[2] == 'D' && header[3] == 'F',
        ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
        ".png" => read >= 8 && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".doc" or ".xls" or ".ppt" => read >= 8 && header.Take(8).SequenceEqual(new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 }),
        ".docx" or ".xlsx" or ".pptx" => read >= 4 && header[0] == 'P' && header[1] == 'K' && header[2] == 3 && header[3] == 4,
        ".txt" => true,
        _ => false
    };

    private static DocumentValidationResult Invalid(string message) => new(false, message, string.Empty, string.Empty);
}

public static class DocumentCategories
{
    public static readonly string[] All = { "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other" };
}