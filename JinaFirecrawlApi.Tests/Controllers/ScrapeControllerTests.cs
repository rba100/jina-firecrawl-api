using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using JinaFirecrawlApi.Controllers;
using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Controllers;

public class ScrapeControllerTests
{
    private Mock<IScrapeService> _mockScrapeService;
    private ScrapeController _controller;

    [SetUp]
    public void SetUp()
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

    [Test]
    public async Task Scrape_WithMissingAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };

        // Act
        var result = await _controller.Scrape(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.Value, Is.TypeOf<FirecrawlErrorResponse>());
        var response = (FirecrawlErrorResponse)unauthorizedResult.Value!;
        Assert.That(response.Error, Is.EqualTo("Authorization header required"));
    }

    [Test]
    public async Task Scrape_WithEmptyAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        _controller.HttpContext.Request.Headers["Authorization"] = "";

        // Act
        var result = await _controller.Scrape(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.Value, Is.TypeOf<FirecrawlErrorResponse>());
        var response = (FirecrawlErrorResponse)unauthorizedResult.Value!;
        Assert.That(response.Error, Is.EqualTo("Authorization header required"));
    }

    [Test]
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
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.TypeOf<FirecrawlResponse>());
        var response = (FirecrawlResponse)okResult.Value!;
        Assert.That(response.Success, Is.True);
        Assert.That(response.Data?.Markdown, Is.EqualTo("content"));
        Assert.That(response.Metadata?.SourceURL, Is.EqualTo("https://example.com"));
    }

    [Test]
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
        Assert.That(result, Is.TypeOf<ObjectResult>());
        var statusResult = (ObjectResult)result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(503));
        Assert.That(statusResult.Value, Is.TypeOf<FirecrawlErrorResponse>());
        var response = (FirecrawlErrorResponse)statusResult.Value!;
        Assert.That(response.Error, Does.Contain("Request failed for URL:"));
        Assert.That(response.Error, Does.Contain("Network error"));
    }

    [Test]
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

    [Test]
    [TestCase("Bearer token")]
    [TestCase("Basic token")]
    [TestCase("API-KEY token")]
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