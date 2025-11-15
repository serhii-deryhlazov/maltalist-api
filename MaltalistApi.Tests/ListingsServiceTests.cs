using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MaltalistApi.Models;
using MaltalistApi.Services;

namespace MaltalistApi.Tests;

public class FakeFileStorageService : IFileStorageService
{
    public bool DeleteCalled { get; private set; } = false;

    public Task<List<string>> SaveFilesAsync(int listingId, IFormFileCollection files)
    {
        return Task.FromResult(new List<string> { "file1.jpg", "file2.jpg" });
    }

    public Task DeleteFilesAsync(int listingId)
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }
}

public class FakeFormFileCollection : List<IFormFile>, IFormFileCollection
{
    public IFormFile? this[string name] => this.FirstOrDefault();
    public IFormFile? GetFile(string name) => this.FirstOrDefault();
    public IReadOnlyList<IFormFile> GetFiles(string name) => this;
}

public class ListingsServiceTests
{
    private MaltalistDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MaltalistDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MaltalistDbContext(options);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_ReturnsEmptyList_WhenNoListings()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var request = new GetAllListingsRequest { Page = 1, Limit = 10 };
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        Assert.NotNull(result);
        Assert.Empty(result.Listings);
        Assert.Equal(0, result.TotalNumber);
    }

    [Fact]
    public async Task CreateListingAsync_CreatesListing_WithValidData()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var user = new User { Id = "test-user", Email = "test@test.com", UserName = "Test User", UserPicture = "pic.jpg", CreatedAt = DateTime.UtcNow, LastOnline = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new CreateListingRequest
        {
            Title = "Test Listing",
            Description = "Test Description",
            Price = 100.0m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "test-user"
        };

        var result = await service.CreateListingAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Test Listing", result.Title);
        Assert.Equal(100.0m, result.Price);
    }

    [Fact]
    public async Task GetListingByIdAsync_ReturnsListing_WhenExists()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var listing = new Listing
        {
            Title = "Test",
            Description = "Desc",
            Price = 50m,
            Category = "Books",
            Location = "Sliema",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var result = await service.GetListingByIdAsync(listing.Id);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task UpdateListingAsync_UpdatesListing()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var listing = new Listing
        {
            Title = "Old Title",
            Description = "Old Desc",
            Price = 50m,
            Category = "Books",
            Location = "Sliema",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateListingRequest
        {
            Id = listing.Id,
            Title = "New Title",
            Description = "New Desc",
            Price = 75m,
            Category = "Books",
            Location = "Valletta",
            ShowPhone = false,
            Complete = false,
            Lease = false
        };

        var result = await service.UpdateListingAsync(listing.Id, updateRequest);

        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal(75m, result.Price);
    }

    [Fact]
    public async Task DeleteListingAsync_DeletesListing()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var listing = new Listing
        {
            Title = "To Delete",
            Description = "Desc",
            Price = 50m,
            Category = "Books",
            Location = "Sliema",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var result = await service.DeleteListingAsync(listing.Id);

        Assert.True(result);
        Assert.Null(await context.Listings.FindAsync(listing.Id));
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_FiltersByCategory()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Book", Description = "A book", Price = 10m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Phone", Description = "A phone", Price = 200m, Category = "Electronics", Location = "Sliema", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10, Category = "Books" };
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        Assert.Equal(1, result.TotalNumber);
        Assert.Single(result.Listings);
        Assert.Equal("Book", result.Listings.First().Title);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_FiltersBySearch()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "iPhone 12", Description = "Great phone", Price = 500m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Samsung TV", Description = "Big screen", Price = 800m, Category = "Electronics", Location = "Sliema", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10, Search = "iPhone" };
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        Assert.Equal(1, result.TotalNumber);
        Assert.Single(result.Listings);
        Assert.Contains("iPhone", result.Listings.First().Title);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_Paginates()
    {
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        for (int i = 0; i < 15; i++)
        {
            context.Listings.Add(new Listing
            {
                Title = $"Listing {i}",
                Description = "Desc",
                Price = 10m,
                Category = "Books",
                Location = "Valletta",
                UserId = "user1",
                Approved = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 2, Limit = 10 };
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        Assert.Equal(15, result.TotalNumber);
        Assert.Equal(5, result.Listings.Count);
        Assert.Equal(2, result.Page);
    }
}
