using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentUploadResult>> UploadAsync(IEnumerable<IBrowserFile> files, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsAsync(int userId, DocumentQuery? query = null, CancellationToken cancellationToken = default);
    Task<Document?> GetAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<Stream?> OpenAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default);
    Task<bool> ReplaceAsync(int documentId, IBrowserFile file, int userId, CancellationToken cancellationToken = default);
    Task RecordDownloadAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default);
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IMalwareScanner _scanner;
    private readonly IDocumentValidationService _validation;
    private readonly IDocumentAuthorizationService _authorization;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IMalwareScanner scanner, IDocumentValidationService validation, IDocumentAuthorizationService authorization)
    { _context = context; _storage = storage; _scanner = scanner; _validation = validation; _authorization = authorization; }

    public async Task<IReadOnlyList<DocumentUploadResult>> UploadAsync(IEnumerable<IBrowserFile> files, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var results = new List<DocumentUploadResult>();
        foreach (var file in files)
        {
            string? temporaryKey = null;
            string? storageKey = null;
            try
            {
                var validation = await _validation.ValidateAsync(file, request, cancellationToken);
                if (!validation.IsValid) { results.Add(new(file.Name, false, validation.Error!)); continue; }
                await using var input = file.OpenReadStream(DocumentConstants.MaxFileSize, cancellationToken);
                var scan = await _scanner.ScanAsync(input, cancellationToken);
                if (scan.Status != MalwareScanStatus.Available) { results.Add(new(file.Name, false, scan.Reason!)); continue; }
                await using var content = file.OpenReadStream(DocumentConstants.MaxFileSize, cancellationToken);
                temporaryKey = await _storage.WriteTemporaryAsync(content, validation.Extension, cancellationToken);
                storageKey = $"documents/{Guid.NewGuid():N}{validation.Extension}";
                await _storage.PromoteAsync(temporaryKey, storageKey, cancellationToken);
                var document = new Document { Title = request.Title.Trim(), Description = request.Description, Category = request.Category, Tags = request.Tags, OriginalFileName = file.Name, StorageKey = storageKey, FileType = validation.ContentType, FileSize = file.Size, UploaderId = userId, ProjectId = request.ProjectId, TaskId = request.TaskId, Status = DocumentStatus.Ready };
                _context.Documents.Add(document);
                _context.DocumentActivities.Add(new DocumentActivity { Document = document, ActorUserId = userId, Action = "Upload", FileType = document.FileType });
                await _context.SaveChangesAsync(cancellationToken);
                results.Add(new(file.Name, true, "Document uploaded successfully.", document));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or DbUpdateException)
            {
                if (storageKey != null) await _storage.DeleteAsync(storageKey, cancellationToken);
                if (temporaryKey != null) await _storage.DeleteAsync(temporaryKey, cancellationToken);
                results.Add(new(file.Name, false, "The document could not be stored safely."));
            }
        }
        return results;
    }

    public async Task<List<Document>> GetDocumentsAsync(int userId, DocumentQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new();
        var documents = await _context.Documents.AsNoTracking().Include(d => d.Uploader).Include(d => d.Project).Where(d => d.Status == DocumentStatus.Ready).ToListAsync(cancellationToken);
        var authorized = new List<Document>();
        foreach (var document in documents) if (await _authorization.CanAccessAsync(document, userId, cancellationToken)) authorized.Add(document);
        if (!string.IsNullOrWhiteSpace(query.Search)) authorized = authorized.Where(d => (d.Title + " " + d.Description + " " + d.Tags + " " + d.Uploader.DisplayName + " " + d.Project?.Name).Contains(query.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(query.Category)) authorized = authorized.Where(d => d.Category == query.Category).ToList();
        if (query.ProjectId.HasValue) authorized = authorized.Where(d => d.ProjectId == query.ProjectId).ToList();
        if (query.From.HasValue) authorized = authorized.Where(d => d.UploadedDate >= query.From.Value).ToList();
        if (query.To.HasValue) authorized = authorized.Where(d => d.UploadedDate <= query.To.Value).ToList();
        authorized = query.SortBy.ToLowerInvariant() switch { "title" => authorized.OrderBy(d => d.Title).ToList(), "category" => authorized.OrderBy(d => d.Category).ToList(), "size" => authorized.OrderBy(d => d.FileSize).ToList(), _ => authorized.OrderBy(d => d.UploadedDate).ToList() };
        if (query.Descending) authorized.Reverse();
        return authorized;
    }

    public async Task<Document?> GetAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.Uploader).Include(d => d.Project).FirstOrDefaultAsync(d => d.DocumentId == documentId && d.Status == DocumentStatus.Ready, cancellationToken);
        return document != null && await _authorization.CanAccessAsync(document, userId, cancellationToken) ? document : null;
    }

    public async Task<Stream?> OpenAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await GetAuthorizedAsync(documentId, userId, cancellationToken);
        return document == null ? null : await _storage.OpenReadAsync(document.StorageKey, cancellationToken);
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanEditMetadataAsync(document, userId, cancellationToken) || string.IsNullOrWhiteSpace(request.Title) || !DocumentCategories.All.Contains(request.Category, StringComparer.Ordinal)) return false;
        document.Title = request.Title.Trim(); document.Description = request.Description; document.Category = request.Category; document.Tags = request.Tags;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, ActorUserId = userId, Action = "MetadataEdit" });
        await _context.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<bool> ReplaceAsync(int documentId, IBrowserFile file, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId, cancellationToken)) return false;
        var request = new DocumentUploadRequest { Title = document.Title, Category = document.Category, Description = document.Description, Tags = document.Tags };
        var validation = await _validation.ValidateAsync(file, request, cancellationToken);
        if (!validation.IsValid) return false;
        await using var scanStream = file.OpenReadStream(DocumentConstants.MaxFileSize, cancellationToken);
        if ((await _scanner.ScanAsync(scanStream, cancellationToken)).Status != MalwareScanStatus.Available) return false;
        var oldKey = document.StorageKey;
        await using var content = file.OpenReadStream(DocumentConstants.MaxFileSize, cancellationToken);
        var temporaryKey = await _storage.WriteTemporaryAsync(content, validation.Extension, cancellationToken);
        var newKey = $"documents/{Guid.NewGuid():N}{validation.Extension}";
        await _storage.PromoteAsync(temporaryKey, newKey, cancellationToken);
        document.StorageKey = newKey; document.OriginalFileName = file.Name; document.FileType = validation.ContentType; document.FileSize = file.Size;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, ActorUserId = userId, Action = "Replacement", FileType = document.FileType });
        await _context.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(oldKey, cancellationToken);
        return true;
    }

    public async Task RecordDownloadAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        if (await GetAuthorizedAsync(documentId, userId, cancellationToken) is null) return;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, ActorUserId = userId, Action = "Download" });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId, cancellationToken)) return false;
        var key = document.StorageKey; _context.Documents.Remove(document); _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, ActorUserId = userId, Action = "Delete" });
        await _context.SaveChangesAsync(cancellationToken); await _storage.DeleteAsync(key, cancellationToken); return true;
    }
}