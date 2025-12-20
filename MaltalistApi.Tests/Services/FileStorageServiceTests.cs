using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MaltalistApi.Services;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MaltalistApi.Tests.Services;

public class FileStorageServiceTests
{
    private IConfiguration CreateConfiguration(string basePath = "/tmp/test-files")
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"FileStorage:BasePath", basePath}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    private FileStorageService CreateService(IConfiguration config)
    {
        var mockLogger = new Mock<ILogger<FileStorageService>>();
        return new FileStorageService(config, mockLogger.Object);
    }

    private byte[] CreateValidImageBytes(string format = "jpeg")
    {
        using var image = new Image<Rgba32>(100, 100);
        using var ms = new MemoryStream();
        
        if (format == "jpeg")
            image.SaveAsJpeg(ms);
        else if (format == "png")
            image.SaveAsPng(ms);
        else if (format == "webp")
            image.SaveAsWebp(ms);
        
        return ms.ToArray();
    }

    private IFormFile CreateMockFile(string fileName, string contentType, byte[] content)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(content.Length);
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                var ms = new MemoryStream(content);
                return ms.CopyToAsync(target, token);
            });
        return mock.Object;
    }

    [Fact]
    public async Task SaveFilesAsync_ValidImage_SavesSuccessfully()
    {
        // Arrange
        var uniquePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = CreateConfiguration(uniquePath);
        var service = CreateService(config);
        var content = CreateValidImageBytes("jpeg");
        var file = CreateMockFile("test.jpg", "image/jpeg", content);
        
        var files = new FormFileCollection { file };

        // Act
        var result = await service.SaveFilesAsync(1, files);

        // Assert
        Assert.Single(result);
        Assert.Equal("Picture1.jpg", result[0]);

        // Cleanup
        if (Directory.Exists(uniquePath))
            Directory.Delete(uniquePath, true);
    }

    [Fact]
    public async Task SaveFilesAsync_FileTooLarge_ThrowsException()
    {
        // Arrange
        var config = CreateConfiguration();
        var service = CreateService(config);
        var content = new byte[6 * 1024 * 1024]; // 6MB
        var file = CreateMockFile("large.jpg", "image/jpeg", content);
        var files = new FormFileCollection { file };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveFilesAsync(1, files)
        );
        Assert.Contains("exceeds maximum size", exception.Message);
    }

    [Fact]
    public async Task SaveFilesAsync_InvalidExtension_ThrowsException()
    {
        // Arrange
        var config = CreateConfiguration();
        var service = CreateService(config);
        var content = new byte[] { 0x00, 0x01 };
        var file = CreateMockFile("test.exe", "application/x-msdownload", content);
        var files = new FormFileCollection { file };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveFilesAsync(1, files)
        );
        Assert.Contains("not allowed", exception.Message);
    }

    [Fact]
    public async Task SaveFilesAsync_InvalidMimeType_ThrowsException()
    {
        // Arrange
        var config = CreateConfiguration();
        var service = CreateService(config);
        var content = new byte[] { 0x00, 0x01 };
        var file = CreateMockFile("test.jpg", "application/pdf", content);
        var files = new FormFileCollection { file };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveFilesAsync(1, files)
        );
        Assert.Contains("Invalid MIME type", exception.Message);
    }

    [Fact]
    public async Task SaveFilesAsync_MultipleFiles_SavesWithSequentialNaming()
    {
        // Arrange
        var uniquePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = CreateConfiguration(uniquePath);
        var service = CreateService(config);
        
        var files = new FormFileCollection
        {
            CreateMockFile("test1.jpg", "image/jpeg", CreateValidImageBytes("jpeg")),
            CreateMockFile("test2.png", "image/png", CreateValidImageBytes("png")),
            CreateMockFile("test3.webp", "image/webp", CreateValidImageBytes("webp"))
        };

        // Act
        var result = await service.SaveFilesAsync(1, files);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Picture1.jpg", result[0]);
        Assert.Equal("Picture2.png", result[1]);
        Assert.Equal("Picture3.webp", result[2]);

        // Cleanup
        if (Directory.Exists(uniquePath))
            Directory.Delete(uniquePath, true);
    }

    [Fact]
    public async Task SaveFilesAsync_MoreThan10Files_SavesOnlyFirst10()
    {
        // Arrange
        var uniquePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = CreateConfiguration(uniquePath);
        var service = CreateService(config);
        var content = CreateValidImageBytes("jpeg");
        
        var files = new FormFileCollection();
        for (int i = 0; i < 15; i++)
        {
            files.Add(CreateMockFile($"test{i}.jpg", "image/jpeg", content));
        }

        // Act
        var result = await service.SaveFilesAsync(1, files);

        // Assert
        Assert.Equal(10, result.Count);

        // Cleanup
        if (Directory.Exists(uniquePath))
            Directory.Delete(uniquePath, true);
    }

    [Fact]
    public async Task DeleteFilesAsync_ExistingDirectory_DeletesSuccessfully()
    {
        // Arrange
        var uniquePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = CreateConfiguration(uniquePath);
        var service = CreateService(config);
        var listingDir = Path.Combine(uniquePath, "listings", "1");
        Directory.CreateDirectory(listingDir);
        File.WriteAllText(Path.Combine(listingDir, "test.jpg"), "test");

        // Act
        await service.DeleteFilesAsync(1);

        // Assert
        Assert.False(Directory.Exists(listingDir));

        // Cleanup
        if (Directory.Exists(uniquePath))
            Directory.Delete(uniquePath, true);
    }

    [Fact]
    public async Task DeleteFilesAsync_NonexistentDirectory_DoesNotThrow()
    {
        // Arrange
        var config = CreateConfiguration();
        var service = CreateService(config);

        // Act & Assert - should not throw
        await service.DeleteFilesAsync(999);
    }

    [Fact]
    public async Task SaveFilesAsync_AcceptsAllAllowedImageTypes()
    {
        // Arrange
        var uniquePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = CreateConfiguration(uniquePath);
        var service = CreateService(config);
        
        var files = new FormFileCollection
        {
            CreateMockFile("test.jpg", "image/jpeg", CreateValidImageBytes("jpeg")),
            CreateMockFile("test.jpeg", "image/jpeg", CreateValidImageBytes("jpeg")),
            CreateMockFile("test.png", "image/png", CreateValidImageBytes("png")),
            CreateMockFile("test.webp", "image/webp", CreateValidImageBytes("webp"))
        };

        // Act
        var result = await service.SaveFilesAsync(1, files);

        // Assert
        Assert.Equal(4, result.Count);

        // Cleanup
        if (Directory.Exists(uniquePath))
            Directory.Delete(uniquePath, true);
    }
}
