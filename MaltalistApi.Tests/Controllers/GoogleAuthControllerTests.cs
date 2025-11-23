using Xunit;
using Microsoft.EntityFrameworkCore;
using MaltalistApi.Controllers;
using MaltalistApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MaltalistApi.Services;

namespace MaltalistApi.Tests.Controllers;

public class GoogleAuthControllerTests
{
    private MaltalistDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MaltalistDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MaltalistDbContext(options);
    }

    [Fact]
    public async Task GoogleLogin_NullToken_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var mockLogger = new Mock<ILogger<GoogleAuthController>>();
        var mockUsersService = new Mock<IUsersService>();
        var controller = new GoogleAuthController(context, mockLogger.Object, mockUsersService.Object);
        var request = new GoogleAuthController.GoogleLoginRequest { IdToken = null! };

        // Act
        var result = await controller.LoginWithGoogle(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GoogleLogin_EmptyToken_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var mockLogger = new Mock<ILogger<GoogleAuthController>>();
        var mockUsersService = new Mock<IUsersService>();
        var controller = new GoogleAuthController(context, mockLogger.Object, mockUsersService.Object);
        var request = new GoogleAuthController.GoogleLoginRequest { IdToken = "" };

        // Act
        var result = await controller.LoginWithGoogle(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GoogleLogin_WhitespaceToken_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var mockLogger = new Mock<ILogger<GoogleAuthController>>();
        var mockUsersService = new Mock<IUsersService>();
        var controller = new GoogleAuthController(context, mockLogger.Object, mockUsersService.Object);
        var request = new GoogleAuthController.GoogleLoginRequest { IdToken = "   " };

        // Act
        var result = await controller.LoginWithGoogle(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GoogleLogin_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        using var context = CreateContext();
        var mockLogger = new Mock<ILogger<GoogleAuthController>>();
        var mockUsersService = new Mock<IUsersService>();
        var controller = new GoogleAuthController(context, mockLogger.Object, mockUsersService.Object);
        var request = new GoogleAuthController.GoogleLoginRequest { IdToken = "invalid-token" };

        // Act
        var result = await controller.LoginWithGoogle(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    // Note: Testing with valid Google tokens requires integration tests
    // as GoogleJsonWebSignature.ValidateAsync makes actual calls to Google's APIs
}
