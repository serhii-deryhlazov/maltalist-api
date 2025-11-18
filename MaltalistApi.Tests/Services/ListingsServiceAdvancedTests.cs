using Xunit;
using Microsoft.EntityFrameworkCore;
using MaltalistApi.Models;
using MaltalistApi.Services;

namespace MaltalistApi.Tests.Services;

public class ListingsServiceAdvancedTests
{
    private MaltalistDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MaltalistDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MaltalistDbContext(options);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_SortsByPriceAscending()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Expensive", Description = "Desc", Price = 1000m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Cheap", Description = "Desc", Price = 10m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Medium", Description = "Desc", Price = 100m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10, Sort = "price_asc" };

        // Act
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        // Assert
        Assert.Equal(3, result.TotalNumber);
        Assert.Equal("Cheap", result.Listings[0].Title);
        Assert.Equal("Medium", result.Listings[1].Title);
        Assert.Equal("Expensive", result.Listings[2].Title);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_SortsByPriceDescending()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Expensive", Description = "Desc", Price = 1000m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Cheap", Description = "Desc", Price = 10m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10, Sort = "price_desc" };

        // Act
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        // Assert
        Assert.Equal("Expensive", result.Listings[0].Title);
        Assert.Equal("Cheap", result.Listings[1].Title);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_SortsByDateAscending()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Newest", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Oldest", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Middle", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10, Sort = "date_asc" };

        // Act
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        // Assert
        Assert.Equal("Oldest", result.Listings[0].Title);
        Assert.Equal("Middle", result.Listings[1].Title);
        Assert.Equal("Newest", result.Listings[2].Title);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_SortsByDateDescending()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Newest", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Oldest", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10, Sort = "date_desc" };

        // Act
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        // Assert
        Assert.Equal("Newest", result.Listings[0].Title);
        Assert.Equal("Oldest", result.Listings[1].Title);
    }

    [Fact]
    public async Task GetMinimalListingsPaginatedAsync_OnlyReturnsApprovedListings()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Approved", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Not Approved", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var request = new GetAllListingsRequest { Page = 1, Limit = 10 };

        // Act
        var result = await service.GetMinimalListingsPaginatedAsync(request);

        // Assert
        Assert.Single(result.Listings);
        Assert.Equal("Approved", result.Listings[0].Title);
    }

    [Fact]
    public async Task CreateListingAsync_AppliesInputSanitization()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var user = new User
        {
            Id = "user1",
            UserName = "Test",
            Email = "test@test.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new CreateListingRequest
        {
            Title = "<script>alert('xss')</script>",
            Description = "Test & Description",
            Price = 100m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1"
        };

        // Act
        var result = await service.CreateListingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("<script>", result.Title);
        Assert.Contains("&amp;", result.Description);
    }

    [Fact]
    public async Task CreateListingAsync_SetsApprovedToFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var user = new User
        {
            Id = "user1",
            UserName = "Test",
            Email = "test@test.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new CreateListingRequest
        {
            Title = "Test",
            Description = "Description",
            Price = 100m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1"
        };

        // Act
        var result = await service.CreateListingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Approved);
    }

    [Fact]
    public async Task UpdateListingAsync_ResetsApprovedToFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var listing = new Listing
        {
            Title = "Original",
            Description = "Desc",
            Price = 100m,
            Category = "Books",
            Location = "Valletta",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var request = new UpdateListingRequest
        {
            Title = "Updated",
            Description = "Updated Desc",
            Price = 150m,
            Category = "Books",
            Location = "Valletta"
        };

        // Act
        var result = await service.UpdateListingAsync(listing.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Approved);
    }

    [Fact]
    public async Task UpdateListingAsync_AppliesInputSanitization()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        var listing = new Listing
        {
            Title = "Original",
            Description = "Desc",
            Price = 100m,
            Category = "Books",
            Location = "Valletta",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var request = new UpdateListingRequest
        {
            Title = "<b>Bold Title</b>",
            Description = "Test & Description",
            Price = 150m,
            Category = "Books",
            Location = "Valletta"
        };

        // Act
        var result = await service.UpdateListingAsync(listing.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("<b>", result.Title);
        Assert.Contains("&amp;", result.Description);
    }

    [Fact]
    public async Task DeleteListingAsync_CallsFileStorageDelete()
    {
        // Arrange
        using var context = CreateContext();
        var fakeStorage = new FakeFileStorageService();
        var service = new ListingsService(context, fakeStorage);

        var listing = new Listing
        {
            Title = "To Delete",
            Description = "Desc",
            Price = 100m,
            Category = "Books",
            Location = "Valletta",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        // Act
        await service.DeleteListingAsync(listing.Id);

        // Assert
        Assert.True(fakeStorage.DeleteCalled);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        using var context = CreateContext();
        var service = new ListingsService(context, new FakeFileStorageService());

        context.Listings.AddRange(
            new Listing { Title = "Item1", Description = "Desc", Price = 100m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Item2", Description = "Desc", Price = 100m, Category = "Electronics", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Listing { Title = "Item3", Description = "Desc", Price = 100m, Category = "Books", Location = "Valletta", UserId = "user1", Approved = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Electronics", result);
        Assert.Contains("Books", result);
    }
}
