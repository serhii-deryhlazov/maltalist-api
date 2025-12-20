using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MaltalistApi.Controllers;
using MaltalistApi.Models;
using MaltalistApi.Interfaces;
using Moq;
using System.Security.Claims;

namespace MaltalistApi.Tests.Controllers;

public class PromotionsControllerTests
{
    private MaltalistDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MaltalistDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MaltalistDbContext(options);
    }

    private PromotionsController CreateController(MaltalistDbContext context)
    {
        var mockPaymentService = new Mock<IPaymentService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<PromotionsController>>();
        
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.Value).Returns("9.99");
        mockConfiguration.Setup(c => c.GetSection("Stripe:PromotionPrice")).Returns(mockConfigSection.Object);
        mockConfiguration.Setup(c => c["Stripe:PromotionPrice"]).Returns("9.99");
        
        // Mock payment verification to return true
        mockPaymentService.Setup(p => p.VerifyPaymentIntentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
        
        return new PromotionsController(
            context,
            mockPaymentService.Object,
            mockConfiguration.Object,
            mockLogger.Object
        );
    }

    private void SetupAuthenticatedUser(PromotionsController controller, string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetPromotedListings_ReturnsEmptyList_WhenNoPromotions()
    {
        // Arrange
        using var context = CreateContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.GetPromotedListings("Electronics");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsType<List<Listing>>(okResult.Value);
        Assert.Empty(listings);
    }

    [Fact]
    public async Task GetPromotedListings_ReturnsActivePromotions_FiltersByCategory()
    {
        // Arrange
        using var context = CreateContext();
        var controller = CreateController(context);

        var listing1 = new Listing
        {
            Title = "iPhone",
            Description = "Phone",
            Price = 500m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var listing2 = new Listing
        {
            Title = "Book",
            Description = "Novel",
            Price = 20m,
            Category = "Books",
            Location = "Sliema",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.AddRange(listing1, listing2);
        await context.SaveChangesAsync();

        var promotion1 = new Promotion
        {
            ListingId = listing1.Id,
            Category = "Electronics",
            ExpirationDate = DateTime.UtcNow.AddDays(7)
        };
        var promotion2 = new Promotion
        {
            ListingId = listing2.Id,
            Category = "Books",
            ExpirationDate = DateTime.UtcNow.AddDays(7)
        };
        context.Promotions.AddRange(promotion1, promotion2);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPromotedListings("Electronics");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsType<List<Listing>>(okResult.Value);
        Assert.Single(listings);
        Assert.Equal("iPhone", listings.First().Title);
    }

    [Fact]
    public async Task GetPromotedListings_FiltersExpiredPromotions()
    {
        // Arrange
        using var context = CreateContext();
        var controller = CreateController(context);

        var listing1 = new Listing
        {
            Title = "Active",
            Description = "Active Listing",
            Price = 100m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var listing2 = new Listing
        {
            Title = "Expired",
            Description = "Expired Listing",
            Price = 200m,
            Category = "Electronics",
            Location = "Sliema",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.AddRange(listing1, listing2);
        await context.SaveChangesAsync();

        var activePromotion = new Promotion
        {
            ListingId = listing1.Id,
            Category = "Electronics",
            ExpirationDate = DateTime.UtcNow.AddDays(7)
        };
        var expiredPromotion = new Promotion
        {
            ListingId = listing2.Id,
            Category = "Electronics",
            ExpirationDate = DateTime.UtcNow.AddDays(-1)
        };
        context.Promotions.AddRange(activePromotion, expiredPromotion);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetPromotedListings("Electronics");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var listings = Assert.IsType<List<Listing>>(okResult.Value);
        Assert.Single(listings);
        Assert.Equal("Active", listings.First().Title);
    }

    [Fact]
    public async Task CreatePromotion_ReturnsCreatedResult_WithValidData()
    {
        // Arrange
        using var context = CreateContext();
        var controller = CreateController(context);
        SetupAuthenticatedUser(controller, "user1");

        var listing = new Listing
        {
            Title = "Test",
            Description = "Test Listing",
            Price = 100m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var request = new CreatePromotionRequest
        {
            ListingId = listing.Id,
            Category = "Electronics",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            PaymentIntentId = "pi_test_123456789"
        };

        // Act
        var result = await controller.CreatePromotion(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var promotion = Assert.IsType<Promotion>(createdResult.Value);
        Assert.Equal(listing.Id, promotion.ListingId);
        Assert.Equal("Electronics", promotion.Category);
    }

    [Fact]
    public async Task CreatePromotion_SavesToDatabase()
    {
        // Arrange
        using var context = CreateContext();
        var controller = CreateController(context);
        SetupAuthenticatedUser(controller, "user1");

        var listing = new Listing
        {
            Title = "Test",
            Description = "Test Listing",
            Price = 100m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1",
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var request = new CreatePromotionRequest
        {
            ListingId = listing.Id,
            Category = "Electronics",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            PaymentIntentId = "pi_test_123456789"
        };

        // Act
        await controller.CreatePromotion(request);

        // Assert
        var savedPromotion = await context.Promotions.FirstOrDefaultAsync(p => p.ListingId == listing.Id);
        Assert.NotNull(savedPromotion);
        Assert.Equal("Electronics", savedPromotion.Category);
    }
}
