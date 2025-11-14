using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MaltalistApi.Tests;

public class PicturesServiceTests
{
    private MaltalistDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MaltalistDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MaltalistDbContext(options);
    }

    [Fact]
    public async Task AddListingPicturesAsync_ThrowsException_WhenListingNotFound()
    {
        var context = CreateContext();
        var fileStorage = new FakeFileStorageService();
        var service = new PicturesService(context, fileStorage);

        var fakeFiles = new FakeFormFileCollection();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddListingPicturesAsync(999, fakeFiles)
        );
    }

    [Fact]
    public async Task AddListingPicturesAsync_SavesFiles_WhenListingExists()
    {
        var context = CreateContext();
        var fileStorage = new FakeFileStorageService();
        var service = new PicturesService(context, fileStorage);

        var listing = new Listing
        {
            Id = 1,
            Title = "Test Listing",
            Description = "Description",
            Price = 100,
            Category = "Category",
            Location = "Location",
            UserId = "user1",
            ShowPhone = false,
            Complete = false,
            Lease = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var fakeFiles = new FakeFormFileCollection();
        var result = await service.AddListingPicturesAsync(1, fakeFiles);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // FakeFileStorageService returns 2 files
        Assert.Contains("file1.jpg", result);
        Assert.Contains("file2.jpg", result);
    }

    [Fact]
    public async Task UpdateListingPicturesAsync_ThrowsException_WhenListingNotFound()
    {
        var context = CreateContext();
        var fileStorage = new FakeFileStorageService();
        var service = new PicturesService(context, fileStorage);

        var fakeFiles = new FakeFormFileCollection();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateListingPicturesAsync(999, fakeFiles)
        );
    }

    [Fact]
    public async Task UpdateListingPicturesAsync_DeletesAndSavesFiles_WhenListingExists()
    {
        var context = CreateContext();
        var fileStorage = new FakeFileStorageService();
        var service = new PicturesService(context, fileStorage);

        var listing = new Listing
        {
            Id = 1,
            Title = "Test Listing",
            Description = "Description",
            Price = 100,
            Category = "Category",
            Location = "Location",
            UserId = "user1",
            ShowPhone = false,
            Complete = false,
            Lease = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var fakeFiles = new FakeFormFileCollection();
        var result = await service.UpdateListingPicturesAsync(1, fakeFiles);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("file1.jpg", result);
        Assert.Contains("file2.jpg", result);
        Assert.True(fileStorage.DeleteCalled); // Verify delete was called
    }
}
