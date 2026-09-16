using Microsoft.AspNetCore.Components.Forms;
using Xunit;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests;

public class DocumentUploadValidationTests
{
    [Fact]
    public async Task AcceptsPdfAtExactMaximumSize()
    {
        var file = new TestBrowserFile("report.pdf", "application/pdf", 25 * 1024 * 1024, new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' });
        var result = await new DocumentValidationService().ValidateAsync(file, new DocumentUploadRequest { Title = "Report", Category = "Reports" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RejectsMismatchedMimeAndSignature()
    {
        var file = new TestBrowserFile("report.pdf", "image/png", 4, new byte[] { 1, 2, 3, 4 });
        var result = await new DocumentValidationService().ValidateAsync(file, new DocumentUploadRequest { Title = "Report", Category = "Reports" });
        Assert.False(result.IsValid);
        Assert.Contains("MIME", result.Error);
    }

    private sealed class TestBrowserFile : IBrowserFile
    {
        private readonly byte[] _content;
        public TestBrowserFile(string name, string contentType, long size, byte[] content) { Name = name; ContentType = contentType; Size = size; _content = content; }
        public string Name { get; }
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size { get; }
        public string ContentType { get; }
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(_content);
    }
}