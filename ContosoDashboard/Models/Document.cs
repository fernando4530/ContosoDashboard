using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class Document
{
    public int DocumentId { get; set; }
    [Required, MaxLength(255)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(50)] public string Category { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Tags { get; set; }
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string FileType { get; set; } = string.Empty;
    [Range(1, DocumentConstants.MaxFileSize)] public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public int UploaderId { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public User Uploader { get; set; } = null!;
    public Project? Project { get; set; }
    public TaskItem? Task { get; set; }
    public ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
}

public enum DocumentStatus { Pending, Ready, Failed }

public static class DocumentConstants
{
    public const long MaxFileSize = 25 * 1024 * 1024;
    public const string AvailableScanner = "Available";
    public const string RejectedScanner = "Rejected";
    public const string UnavailableScanner = "Unavailable";
}