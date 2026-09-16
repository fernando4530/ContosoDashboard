using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    public int DocumentActivityId { get; set; }
    public int? DocumentId { get; set; }
    public int ActorUserId { get; set; }
    [Required, MaxLength(50)] public string Action { get; set; } = string.Empty;
    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;
    [MaxLength(2000)] public string? Details { get; set; }
    [MaxLength(255)] public string? FileType { get; set; }
    public Document? Document { get; set; }
    public User ActorUser { get; set; } = null!;
}