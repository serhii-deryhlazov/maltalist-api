using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using MaltalistApi.Controllers;
using MaltalistApi.Models;
using MaltalistApi.Services;
using System.Security.Claims;

namespace MaltalistApi.Tests.Controllers;

public class UserProfileControllerTests
{
    private readonly Mock<IUsersService> _mockService;
    private readonly UserProfileController _controller;

    public UserProfileControllerTests()
    {
        _mockService = new Mock<IUsersService>();
        _controller = new UserProfileController(_mockService.Object);
    }

    private void SetupAuthenticatedUser(string userId)
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
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetUserProfile_ReturnsOkResult_WhenUserExists()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            UserName = "John Doe",
            Email = "john@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetUserByIdAsync("user1"))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserProfile("user1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<User>(okResult.Value);
        Assert.Equal("user1", returnValue.Id);
    }

    [Fact]
    public async Task GetUserProfile_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetUserByIdAsync("invalid"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUserProfile("invalid");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateUserProfile_ReturnsOkResult_WithUpdatedUser()
    {
        // Arrange
        SetupAuthenticatedUser("user1");
        var updatedUser = new User
        {
            Id = "user1",
            UserName = "Updated Name",
            Email = "updated@example.com",
            UserPicture = "new.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        _mockService.Setup(s => s.UpdateUserAsync("user1", updatedUser))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateUserProfile("user1", updatedUser);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<User>(okResult.Value);
        Assert.Equal("Updated Name", returnValue.UserName);
    }

    [Fact]
    public async Task UpdateUserProfile_ReturnsBadRequest_WhenIdMismatch()
    {
        // Arrange
        SetupAuthenticatedUser("user1");
        var updatedUser = new User
        {
            Id = "user2",
            UserName = "Name",
            Email = "email@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };

        // Act
        var result = await _controller.UpdateUserProfile("user1", updatedUser);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateUserProfile_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupAuthenticatedUser("invalid");
        var updatedUser = new User
        {
            Id = "invalid",
            UserName = "Name",
            Email = "email@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        _mockService.Setup(s => s.UpdateUserAsync("invalid", updatedUser))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.UpdateUserProfile("invalid", updatedUser);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteUserProfile_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        SetupAuthenticatedUser("user1");
        _mockService.Setup(s => s.DeleteUserAsync("user1"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteUserProfile("user1");

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUserProfile_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupAuthenticatedUser("invalid");
        _mockService.Setup(s => s.DeleteUserAsync("invalid"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteUserProfile("invalid");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task CreateUserProfile_ReturnsCreatedResult_WithValidData()
    {
        // Arrange
        var newUser = new User
        {
            Id = "user1",
            UserName = "John Doe",
            Email = "john@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        _mockService.Setup(s => s.CreateUserAsync(newUser))
            .ReturnsAsync(newUser);

        // Act
        var result = await _controller.CreateUserProfile(newUser);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnValue = Assert.IsType<User>(createdResult.Value);
        Assert.Equal("user1", returnValue.Id);
    }

    [Fact]
    public async Task CreateUserProfile_ReturnsBadRequest_WhenUserNameIsEmpty()
    {
        // Arrange
        var newUser = new User
        {
            Id = "user1",
            UserName = "",
            Email = "john@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };

        // Act
        var result = await _controller.CreateUserProfile(newUser);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadProfilePicture_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupAuthenticatedUser("invalid");
        _mockService.Setup(s => s.GetUserByIdAsync("invalid"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.UploadProfilePicture("invalid");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task UploadProfilePicture_ReturnsBadRequest_WhenNoFileProvided()
    {
        // Arrange
        SetupAuthenticatedUser("user1");
        var user = new User
        {
            Id = "user1",
            UserName = "John",
            Email = "john@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetUserByIdAsync("user1"))
            .ReturnsAsync(user);

        _controller.ControllerContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        // Act
        var result = await _controller.UploadProfilePicture("user1");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadProfilePicture_ReturnsOk_WithSuccessfulUpload()
    {
        // Arrange
        SetupAuthenticatedUser("user1");
        var user = new User
        {
            Id = "user1",
            UserName = "John",
            Email = "john@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetUserByIdAsync("user1"))
            .ReturnsAsync(user);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.FileName).Returns("profile.jpg");

        var files = new FormFileCollection { mockFile.Object };
        _controller.ControllerContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), files);

        _mockService.Setup(s => s.UploadUserProfilePictureAsync("user1", It.IsAny<IFormFile>()))
            .ReturnsAsync("/assets/img/users/user1/profile.jpg");

        // Act
        var result = await _controller.UploadProfilePicture("user1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
