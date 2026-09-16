using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ContosoDashboard.Services;

namespace ContosoDashboard.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/documents").RequireAuthorization("Employee");
        group.MapGet("/{documentId:int}/download", DownloadAsync);
        group.MapGet("/{documentId:int}/preview", PreviewAsync);
        return endpoints;
    }

    private static async Task<IResult> DownloadAsync(int documentId, ClaimsPrincipal principal, IDocumentService documents, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var document = await documents.GetAuthorizedAsync(documentId, userId, cancellationToken);
        if (document == null) return Results.NotFound();
        var stream = await documents.OpenAuthorizedAsync(documentId, userId, cancellationToken);
        await documents.RecordDownloadAsync(documentId, userId, cancellationToken);
        return stream == null ? Results.NotFound() : Results.File(stream, document.FileType, document.OriginalFileName, enableRangeProcessing: true);
    }

    private static async Task<IResult> PreviewAsync(int documentId, ClaimsPrincipal principal, IDocumentService documents, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var document = await documents.GetAuthorizedAsync(documentId, userId, cancellationToken);
        if (document == null) return Results.NotFound();
        if (document.FileType is not ("application/pdf" or "image/jpeg" or "image/png")) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        var stream = await documents.OpenAuthorizedAsync(documentId, userId, cancellationToken);
        return stream == null ? Results.NotFound() : Results.File(stream, document.FileType, fileDownloadName: null, enableRangeProcessing: true);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out int userId) => int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
}