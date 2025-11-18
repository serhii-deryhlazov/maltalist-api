using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MaltalistApi.Middleware;
using System.Net;
using System.Text.Json;

namespace MaltalistApi.Tests.Middleware;

public class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoException_CallsNextMiddleware()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new UnauthorizedAccessException("Unauthorized");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new ArgumentException("Bad argument");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new KeyNotFoundException("Not found");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new InvalidOperationException("Invalid operation");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ProductionEnvironment_HidesStackTrace()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

        Assert.NotNull(jsonResponse);
        Assert.False(jsonResponse!.ContainsKey("stackTrace"));
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentEnvironment_IncludesStackTrace()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);

        Assert.NotNull(jsonResponse);
        Assert.True(jsonResponse!.ContainsKey("StackTrace"));
    }

    [Fact]
    public async Task InvokeAsync_SetsContentTypeToJson()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        RequestDelegate next = (HttpContext hc) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        var testException = new Exception("Test exception");
        RequestDelegate next = (HttpContext hc) =>
        {
            throw testException;
        };

        var middleware = new ErrorHandlingMiddleware(next, mockLogger.Object, mockEnvironment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
