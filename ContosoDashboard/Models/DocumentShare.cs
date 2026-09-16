using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    public int DocumentShareId { get; set; }
    public int DocumentId { get; set; }
    public int? RecipientUserId { get; set; }
    [MaxLength(100)] public string? RecipientDepartment { get; set; }
    public int SharedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedDate { get; set; }
    public Document Document { get; set; } = null!;
    public User? RecipientUser { get; set; }
    public User SharedByUser { get; set; } = null!;
}