using Xunit;
using MaltalistApi.Models;

namespace MaltalistApi.Tests;

public class ModelTests
{
    [Fact]
    public void Listing_ShouldCreateWithValidData()
    {
        // Arrange & Act
        var listing = new Listing
        {
            Id = 1,
            Title = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Electronics",
            Location = "Malta",
            UserId = "user123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, listing.Id);
        Assert.Equal("Test Product", listing.Title);
        Assert.Equal(99.99m, listing.Price);
        Assert.False(listing.Complete);
    }

    [Fact]
    public void User_ShouldCreateWithRequiredFields()
    {
        // Arrange & Act
        var user = new User
        {
            Id = "user123",
            UserName = "testuser",
            Email = "test@example.com",
            UserPicture = "http://example.com/pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("user123", user.Id);
        Assert.Equal("testuser", user.UserName);
        Assert.Contains("@", user.Email);
    }

    [Fact]
    public void Promotion_ShouldHaveExpirationDate()
    {
        // Arrange & Act
        var promotion = new Promotion
        {
            Id = 1,
            ListingId = 1,
            Category = "Electronics",
            ExpirationDate = DateTime.UtcNow.AddDays(7)
        };

        // Assert
        Assert.True(promotion.ExpirationDate > DateTime.UtcNow);
        Assert.Equal("Electronics", promotion.Category);
    }

    [Fact]
    public void CreateListingRequest_ShouldHaveRequiredFields()
    {
        // Arrange & Act
        var request = new CreateListingRequest
        {
            Title = "New Listing",
            Description = "Description",
            Price = 50m,
            Category = "Books",
            UserId = "user123",
            Location = "Valletta"
        };

        // Assert
        Assert.NotNull(request.Title);
        Assert.True(request.Price > 0);
        Assert.False(request.ShowPhone);
    }

    [Fact]
    public void UpdateListingRequest_ShouldUpdateExistingListing()
    {
        // Arrange & Act
        var request = new UpdateListingRequest
        {
            Id = 1,
            Title = "Updated Title",
            Description = "Updated Description",
            Price = 75m,
            Category = "Updated Category",
            Location = "Updated Location",
            ShowPhone = true,
            Complete = true,
            Lease = false
        };

        // Assert
        Assert.Equal(1, request.Id);
        Assert.Equal("Updated Title", request.Title);
        Assert.True(request.ShowPhone);
        Assert.True(request.Complete);
        Assert.False(request.Lease);
    }

    [Fact]
    public void GetAllListingsRequest_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var request = new GetAllListingsRequest();

        // Assert
        Assert.Equal(1, request.Page);
        Assert.Equal(10, request.Limit);
        Assert.Null(request.Search);
        Assert.Null(request.Category);
        Assert.Null(request.Sort);
        Assert.Null(request.Order);
        Assert.Null(request.Location);
    }

    [Fact]
    public void GetAllListingsRequest_ShouldAcceptCustomParameters()
    {
        // Arrange & Act
        var request = new GetAllListingsRequest
        {
            Page = 2,
            Limit = 20,
            Search = "laptop",
            Category = "Electronics",
            Sort = "price",
            Order = "desc",
            Location = "Malta"
        };

        // Assert
        Assert.Equal(2, request.Page);
        Assert.Equal(20, request.Limit);
        Assert.Equal("laptop", request.Search);
        Assert.Equal("Electronics", request.Category);
        Assert.Equal("price", request.Sort);
        Assert.Equal("desc", request.Order);
        Assert.Equal("Malta", request.Location);
    }
}
