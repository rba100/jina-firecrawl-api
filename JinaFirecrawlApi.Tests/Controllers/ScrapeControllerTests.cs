using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using JinaFirecrawlApi.Controllers;
using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Controllers;

public class ScrapeControllerTests
{
    private readonly Mock<IScrapeService> _mockScrapeService;
    private readonly ScrapeController _controller;

    public ScrapeControllerTests()
    {
        _mockScrapeService = new Mock<IScrapeService>();
        _controller = new ScrapeController(_mockScrapeService.Object);
        
        // Setup HTTP context for headers
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Scrape_WithMissingAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };

        // Act
        var result = await _controller.Scrape(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<FirecrawlErrorResponse>(unauthorizedResult.Value);
        Assert.Equal("Authorization header required", response.Error);
    }

    [Fact]
    public async Task Scrape_WithEmptyAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        _controller.HttpContext.Request.Headers["Authorization"] = "";

        // Act
        var result = await _controller.Scrape(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<FirecrawlErrorResponse>(unauthorizedResult.Value);
        Assert.Equal("Authorization header required", response.Error);
    }

    [Fact]
    public async Task Scrape_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var expectedResponse = new FirecrawlResponse
        {
            Success = true,
            Data = new FirecrawlData { Markdown = "content", Html = "" },
            Metadata = new FirecrawlMetadata { SourceURL = "https://example.com" }
        };

        _controller.HttpContext.Request.Headers["Authorization"] = "Bearer test-token";
        _mockScrapeService.Setup(x => x.ScrapeAsync(It.IsAny<ScrapeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Scrape(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FirecrawlResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("content", response.Data?.Markdown);
        Assert.Equal("https://example.com", response.Metadata?.SourceURL);
    }

    [Fact]
    public async Task Scrape_WithHttpRequestException_ReturnsServiceUnavailable()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var httpException = new HttpRequestException("Network error");

        _controller.HttpContext.Request.Headers["Authorization"] = "Bearer test-token";
        _mockScrapeService.Setup(x => x.ScrapeAsync(It.IsAny<ScrapeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(httpException);

        // Act
        var result = await _controller.Scrape(request, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusResult.StatusCode);
        var response = Assert.IsType<FirecrawlErrorResponse>(statusResult.Value);
        Assert.Contains("Request failed for URL:", response.Error);
        Assert.Contains("Network error", response.Error);
    }

    [Fact]
    public async Task Scrape_PassesCorrectParametersToService()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var authHeader = "Bearer test-token";
        var expectedResponse = new FirecrawlResponse { Success = true };

        _controller.HttpContext.Request.Headers["Authorization"] = authHeader;
        _mockScrapeService.Setup(x => x.ScrapeAsync(It.IsAny<ScrapeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedResponse);

        // Act
        await _controller.Scrape(request, CancellationToken.None);

        // Assert
        _mockScrapeService.Verify(x => x.ScrapeAsync(
            It.Is<ScrapeRequest>(r => r.Url == "https://example.com"),
            authHeader,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Theory]
    [InlineData("Bearer token")]
    [InlineData("Basic token")]
    [InlineData("API-KEY token")]
    public async Task Scrape_WithDifferentAuthorizationSchemes_PassesToService(string authHeader)
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var expectedResponse = new FirecrawlResponse { Success = true };

        _controller.HttpContext.Request.Headers["Authorization"] = authHeader;
        _mockScrapeService.Setup(x => x.ScrapeAsync(It.IsAny<ScrapeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedResponse);

        // Act
        await _controller.Scrape(request, CancellationToken.None);

        // Assert
        _mockScrapeService.Verify(x => x.ScrapeAsync(
            It.IsAny<ScrapeRequest>(),
            authHeader,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}