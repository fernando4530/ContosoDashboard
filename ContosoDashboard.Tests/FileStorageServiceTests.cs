using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests;

public class FileStorageServiceTests
{
    [Fact]
    public async Task WritesOutsideWwwrootAndRejectsTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var environment = new TestEnvironment { ContentRootPath = root, WebRootPath = Path.Combine(root, "wwwroot") };
        var service = new FileStorageService(Options.Create(new DocumentStorageOptions()), environment);
        var temporary = await service.WriteTemporaryAsync(new MemoryStream(new byte[] { 1, 2, 3 }), ".bin", CancellationToken.None);
        Assert.DoesNotContain("wwwroot", Path.GetFullPath(Path.Combine(root, "AppData", "uploads", temporary)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.OpenReadAsync("../outside", CancellationToken.None));
        Directory.Delete(root, true);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}