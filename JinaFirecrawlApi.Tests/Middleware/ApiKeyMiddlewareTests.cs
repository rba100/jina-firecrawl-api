using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using JinaFirecrawlApi.Middleware;

namespace JinaFirecrawlApi.Tests.Middleware;

public class ApiKeyMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly ApiKeyMiddleware _middleware;

    public ApiKeyMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new ApiKeyMiddleware(_mockNext.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithRootPath_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingAuthorizationHeader_Returns401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/v1/scrape";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        Assert.Equal("Authorization header required", errorResponse.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithAuthorizationHeader_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/v1/scrape";
        context.Request.Headers["Authorization"] = "Bearer test-token";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode); // Default status code
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/status")]
    [InlineData("/api/test")]
    public async Task InvokeAsync_WithNonRootPathAndMissingAuth_Returns401(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Theory]
    [InlineData("Bearer token")]
    [InlineData("Basic credentials")]
    [InlineData("API-KEY key")]
    [InlineData("some-random-value")]
    public async Task InvokeAsync_WithDifferentAuthorizationValues_CallsNext(string authValue)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/v1/scrape";
        context.Request.Headers["Authorization"] = authValue;

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyAuthorizationHeader_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/v1/scrape";
        context.Request.Headers["Authorization"] = "";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode); // Default status code since next is called
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceAuthorizationHeader_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/v1/scrape";
        context.Request.Headers["Authorization"] = "   ";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode); // Default status code since next is called
    }
}