using Xunit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MaltalistApi.Controllers;
using MaltalistApi.Models;
using MaltalistApi.Services;

namespace MaltalistApi.Tests.Controllers;

public class ListingsControllerTests
{
    private readonly Mock<IListingsService> _mockService;
    private readonly ListingsController _controller;

    public ListingsControllerTests()
    {
        _mockService = new Mock<IListingsService>();
        _controller = new ListingsController(_mockService.Object);
    }

    [Fact]
    public async Task GetMinimalListingsPaginated_ReturnsOkResult_WithListings()
    {
        // Arrange
        var request = new GetAllListingsRequest { Page = 1, Limit = 10 };
        var response = new GetAllListingsResponse
        {
            Listings = new List<ListingSummaryResponse>
            {
                new ListingSummaryResponse
                {
                    Id = 1,
                    Title = "Test",
                    Description = "Desc",
                    Price = 100m,
                    Category = "Electronics",
                    UserId = "user1",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            },
            TotalNumber = 1,
            Page = 1
        };
        _mockService.Setup(s => s.GetMinimalListingsPaginatedAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetMinimalListingsPaginated(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<GetAllListingsResponse>(okResult.Value);
        Assert.Single(returnValue.Listings);
        Assert.Equal(1, returnValue.TotalNumber);
    }

    [Fact]
    public async Task GetMinimalListingsPaginated_Returns500_OnException()
    {
        // Arrange
        var request = new GetAllListingsRequest { Page = 1, Limit = 10 };
        _mockService.Setup(s => s.GetMinimalListingsPaginatedAsync(request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMinimalListingsPaginated(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetListingById_ReturnsOkResult_WhenListingExists()
    {
        // Arrange
        var listing = new Listing
        {
            Id = 1,
            Title = "Test",
            Description = "Desc",
            Price = 100m,
            Category = "Electronics",
            Location = "Valletta",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetListingByIdAsync(1))
            .ReturnsAsync(listing);

        // Act
        var result = await _controller.GetListingById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Listing>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
    }

    [Fact]
    public async Task GetListingById_ReturnsNotFound_WhenListingDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetListingByIdAsync(999))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _controller.GetListingById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateListing_ReturnsCreatedResult_WithValidData()
    {
        // Arrange
        var request = new CreateListingRequest
        {
            Title = "New Listing",
            Description = "Description",
            Price = 50m,
            Category = "Books",
            Location = "Sliema",
            UserId = "user1"
        };
        var createdListing = new Listing
        {
            Id = 1,
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            Location = request.Location,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _mockService.Setup(s => s.CreateListingAsync(request))
            .ReturnsAsync(createdListing);

        // Act
        var result = await _controller.CreateListing(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnValue = Assert.IsType<Listing>(createdResult.Value);
        Assert.Equal(1, returnValue.Id);
        Assert.Equal("New Listing", returnValue.Title);
    }

    [Fact]
    public async Task CreateListing_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Act
        var result = await _controller.CreateListing(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid listing data", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateListing_ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var request = new CreateListingRequest
        {
            Title = "",
            Description = "Description",
            Price = 50m,
            Category = "Books",
            Location = "Sliema",
            UserId = "user1"
        };

        // Act
        var result = await _controller.CreateListing(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid listing data", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateListing_ReturnsBadRequest_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new CreateListingRequest
        {
            Title = "Test",
            Description = "Description",
            Price = 50m,
            Category = "Books",
            Location = "Sliema",
            UserId = "invalid-user"
        };
        _mockService.Setup(s => s.CreateListingAsync(request))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _controller.CreateListing(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid UserId", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateListing_ReturnsOkResult_WithUpdatedListing()
    {
        // Arrange
        var request = new UpdateListingRequest
        {
            Id = 1,
            Title = "Updated",
            Description = "Updated Desc",
            Price = 75m,
            Category = "Electronics",
            Location = "Valletta"
        };
        var updatedListing = new Listing
        {
            Id = 1,
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            Location = request.Location,
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _mockService.Setup(s => s.UpdateListingAsync(1, request))
            .ReturnsAsync(updatedListing);

        // Act
        var result = await _controller.UpdateListing(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Listing>(okResult.Value);
        Assert.Equal("Updated", returnValue.Title);
    }

    [Fact]
    public async Task UpdateListing_ReturnsNotFound_WhenListingDoesNotExist()
    {
        // Arrange
        var request = new UpdateListingRequest
        {
            Id = 999,
            Title = "Updated",
            Description = "Updated Desc",
            Price = 75m,
            Category = "Electronics",
            Location = "Valletta"
        };
        _mockService.Setup(s => s.UpdateListingAsync(999, request))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _controller.UpdateListing(999, request);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateListing_ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var request = new UpdateListingRequest
        {
            Id = 1,
            Title = "",
            Description = "Description",
            Price = 50m,
            Category = "Books",
            Location = "Sliema"
        };

        // Act
        var result = await _controller.UpdateListing(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid listing data", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteListing_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteListingAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteListing(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteListing_ReturnsNotFound_WhenListingDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteListingAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteListing(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCategories_ReturnsOkResult_WithCategories()
    {
        // Arrange
        var categories = new List<string> { "Electronics", "Books", "Furniture" };
        _mockService.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(3, returnValue.Count);
    }

    [Fact]
    public async Task GetUserListings_ReturnsOkResult_WithListings()
    {
        // Arrange
        var listings = new List<ListingSummaryResponse>
        {
            new ListingSummaryResponse
            {
                Id = 1,
                Title = "Test",
                Description = "Desc",
                Price = 100m,
                Category = "Electronics",
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        _mockService.Setup(s => s.GetUserListingsAsync("user1"))
            .ReturnsAsync(listings);

        // Act
        var result = await _controller.GetUserListings("user1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<List<ListingSummaryResponse>>(okResult.Value);
        Assert.Single(returnValue);
    }

    [Fact]
    public async Task GetUserListings_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetUserListingsAsync("invalid-user"))
            .ReturnsAsync((IEnumerable<ListingSummaryResponse>?)null);

        // Act
        var result = await _controller.GetUserListings("invalid-user");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }
}
