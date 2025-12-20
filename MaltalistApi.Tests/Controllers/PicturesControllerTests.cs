using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MaltalistApi.Controllers;
using MaltalistApi.Services;
using MaltalistApi.Models;
using System.Security.Claims;

namespace MaltalistApi.Tests.Controllers;

public class PicturesControllerTests
{
    private void SetupAuthenticatedUser(PicturesController controller, string userId)
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
    public async Task AddListingPictures_ListingNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockPicturesService = new Mock<IPicturesService>();
        var mockListingsService = new Mock<IListingsService>();
        
        mockListingsService
            .Setup(s => s.GetListingByIdAsync(1))
            .ReturnsAsync((Listing?)null);

        var controller = new PicturesController(mockPicturesService.Object, mockListingsService.Object);

        // Act
        var result = await controller.AddListingPictures(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddListingPictures_NoFilesProvided_ReturnsBadRequest()
    {
        // Arrange
        var mockPicturesService = new Mock<IPicturesService>();
        var mockListingsService = new Mock<IListingsService>();
        
        var listing = new Listing
        {
            Id = 1,
            Title = "Test",
            Description = "Test",
            Price = 100m,
            Category = "Books",
            Location = "Valletta",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockListingsService
            .Setup(s => s.GetListingByIdAsync(1))
            .ReturnsAsync(listing);

        var controller = new PicturesController(mockPicturesService.Object, mockListingsService.Object);
        SetupAuthenticatedUser(controller, "user1");
        
        // Create FormFileCollection with no files
        var formFiles = new FormFileCollection();
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFiles);
        
        controller.ControllerContext.HttpContext.Request.ContentType = "multipart/form-data; boundary=----test";
        controller.ControllerContext.HttpContext.Request.Form = formCollection;

        // Act
        var result = await controller.AddListingPictures(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No files provided", badRequestResult.Value);
    }

    [Fact]
    public void GetListingImageUrls_InvalidIdZero_ReturnsBadRequest()
    {
        // Arrange
        var mockPicturesService = new Mock<IPicturesService>();
        var mockListingsService = new Mock<IListingsService>();
        var controller = new PicturesController(mockPicturesService.Object, mockListingsService.Object);

        // Act
        var result = controller.GetListingImageUrls(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid listing ID", badRequestResult.Value);
    }

    [Fact]
    public void GetListingImageUrls_InvalidIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var mockPicturesService = new Mock<IPicturesService>();
        var mockListingsService = new Mock<IListingsService>();
        var controller = new PicturesController(mockPicturesService.Object, mockListingsService.Object);

        // Act
        var result = controller.GetListingImageUrls(-1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid listing ID", badRequestResult.Value);
    }
}
