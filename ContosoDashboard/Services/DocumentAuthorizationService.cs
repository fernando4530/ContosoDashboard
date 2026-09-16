using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentAuthorizationService
{
    Task<bool> CanAccessAsync(Document document, int userId, CancellationToken cancellationToken = default);
    Task<bool> CanManageAsync(Document document, int userId, CancellationToken cancellationToken = default);
    Task<bool> CanEditMetadataAsync(Document document, int userId, CancellationToken cancellationToken = default);
    Task<bool> CanShareAsync(Document document, int userId, CancellationToken cancellationToken = default);
}

public sealed class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;
    public DocumentAuthorizationService(ApplicationDbContext context) => _context = context;

    public async Task<bool> CanAccessAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null || document.Status != DocumentStatus.Ready) return false;
        if (user.Role == UserRole.Administrator || document.UploaderId == userId) return true;
        if (document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)), cancellationToken)) return true;
        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && s.RevokedDate == null && ((s.RecipientUserId == userId) || (s.RecipientDepartment != null && s.RecipientDepartment == user.Department)), cancellationToken);
    }

    public async Task<bool> CanManageAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        if (document.UploaderId == userId) return true;
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user?.Role == UserRole.Administrator) return true;
        return document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId, cancellationToken);
    }

    public async Task<bool> CanEditMetadataAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        if (await CanManageAsync(document, userId, cancellationToken)) return true;
        return await _context.Users.Where(u => u.UserId == userId).Join(_context.Users, u => u.Department, owner => owner.Department, (u, owner) => new { u, owner })
            .AnyAsync(x => x.u.UserId == userId && x.u.Role == UserRole.TeamLead && x.owner.UserId == document.UploaderId, cancellationToken);
    }

    public async Task<bool> CanShareAsync(Document document, int userId, CancellationToken cancellationToken = default) => document.UploaderId == userId || (await _context.Users.Where(u => u.UserId == userId).Select(u => u.Role).FirstOrDefaultAsync(cancellationToken)) == UserRole.Administrator;
}