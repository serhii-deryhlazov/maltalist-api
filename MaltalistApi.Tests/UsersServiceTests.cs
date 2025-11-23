using Xunit;
using Microsoft.EntityFrameworkCore;
using MaltalistApi.Models;
using MaltalistApi.Services;
using Moq;
using System.Net.Http;

namespace MaltalistApi.Tests;

public class UsersServiceTests
{
    private MaltalistDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MaltalistDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MaltalistDbContext(options);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenExists()
    {
        using var context = CreateContext();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var service = new UsersService(context, mockHttpClientFactory.Object);

        var user = new User
        {
            Id = "user123",
            UserName = "John Doe",
            Email = "john@example.com",
            UserPicture = "pic.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await service.GetUserByIdAsync("user123");

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.UserName);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenNotExists()
    {
        using var context = CreateContext();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var service = new UsersService(context, mockHttpClientFactory.Object);

        var result = await service.GetUserByIdAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesNewUser()
    {
        using var context = CreateContext();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var service = new UsersService(context, mockHttpClientFactory.Object);

        var newUser = new User
        {
            Id = "newuser",
            UserName = "Jane Smith",
            Email = "jane@example.com",
            UserPicture = "avatar.jpg"
        };

        var result = await service.CreateUserAsync(newUser);

        Assert.NotNull(result);
        Assert.Equal("newuser", result.Id);
        Assert.Equal("Jane Smith", result.UserName);
        Assert.Equal("jane@example.com", result.Email);
        
        var userInDb = await context.Users.FindAsync("newuser");
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesExistingUser()
    {
        var context = CreateContext();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var service = new UsersService(context, mockHttpClientFactory.Object);

        var existingUser = new User
        {
            Id = "user456",
            UserName = "Old Name",
            Email = "old@example.com",
            UserPicture = "old.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            LastOnline = DateTime.UtcNow.AddHours(-1)
        };

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var originalLastOnline = existingUser.LastOnline;

        var updatedUser = new User
        {
            Id = "user456",
            UserName = "New Name",
            Email = "new@example.com",
            UserPicture = "new.jpg",
            CreatedAt = existingUser.CreatedAt,
            LastOnline = existingUser.LastOnline
        };

        var result = await service.UpdateUserAsync("user456", updatedUser);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.UserName);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("new.jpg", result.UserPicture);
        Assert.True(result.LastOnline >= originalLastOnline);
    }    [Fact]
    public async Task DeleteUserAsync_DeletesUser()
    {
        using var context = CreateContext();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var service = new UsersService(context, mockHttpClientFactory.Object);

        var user = new User
        {
            Id = "user789",
            UserName = "Test User",
            Email = "test@example.com",
            UserPicture = "test.jpg",
            CreatedAt = DateTime.UtcNow,
            LastOnline = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await service.DeleteUserAsync("user789");

        Assert.True(result);
        Assert.Null(await context.Users.FindAsync("user789"));
    }
}
