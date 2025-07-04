using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Services;

public class ScrapeServiceTests
{
    private readonly Mock<IPdfHandler> _mockPdfHandler;
    private readonly Mock<IJinaHandler> _mockJinaHandler;
    private readonly Mock<ILogger<ScrapeService>> _mockLogger;
    private readonly ScrapeService _scrapeService;

    public ScrapeServiceTests()
    {
        _mockPdfHandler = new Mock<IPdfHandler>();
        _mockJinaHandler = new Mock<IJinaHandler>();
        _mockLogger = new Mock<ILogger<ScrapeService>>();
        _scrapeService = new ScrapeService(_mockPdfHandler.Object, _mockJinaHandler.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ScrapeService(_mockPdfHandler.Object, _mockJinaHandler.Object, null!));
        Assert.Equal("logger", exception.ParamName);
        Assert.Contains("Logger cannot be null", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullPdfHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ScrapeService(null!, _mockJinaHandler.Object, _mockLogger.Object));
        Assert.Equal("pdfHandler", exception.ParamName);
        Assert.Contains("PDF handler cannot be null", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullJinaHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ScrapeService(_mockPdfHandler.Object, null!, _mockLogger.Object));
        Assert.Equal("jinaHandler", exception.ParamName);
        Assert.Contains("Jina handler cannot be null", exception.Message);
    }

    [Fact]
    public async Task ScrapeAsync_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new ScrapeRequest { Url = null };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None));
        Assert.Contains("URL cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task ScrapeAsync_WithPdfUrlAndInvalidScheme_ReturnsErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "file://test.pdf" };

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlErrorResponse>(result);
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.Equal("Invalid URL scheme for PDF. Must be http or https.", errorResponse.Error);
    }

    [Fact]
    public async Task ScrapeAsync_WithValidPdfUrl_ReturnSuccessResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com/test.pdf" };
        var expectedMarkdown = "PDF content as markdown";
        
        _mockPdfHandler.Setup(x => x.Scrape(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedMarkdown);

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlResponse>(result);
        var response = (FirecrawlResponse)result;
        Assert.True(response.Success);
        Assert.Equal(expectedMarkdown, response.Data?.Markdown);
        Assert.Equal(string.Empty, response.Data?.Html);
        Assert.Equal("https://example.com/test.pdf", response.Metadata?.SourceURL);
        Assert.Equal(200, response.Metadata?.StatusCode);
    }

    [Fact]
    public async Task ScrapeAsync_WithPdfUrlAndEmptyContent_ReturnsErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com/test.pdf" };
        
        _mockPdfHandler.Setup(x => x.Scrape(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(default(string));

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlErrorResponse>(result);
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.Contains("An unexpected server error occurred", errorResponse.Error);
        Assert.Contains("Failed to extract content from PDF", errorResponse.Error);
    }

    [Fact]
    public async Task ScrapeAsync_WithRegularUrl_ReturnSuccessResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var expectedMarkdown = "Website content as markdown";
        
        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ReturnsAsync(expectedMarkdown);

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlResponse>(result);
        var response = (FirecrawlResponse)result;
        Assert.True(response.Success);
        Assert.Equal(expectedMarkdown, response.Data?.Markdown);
        Assert.Equal(string.Empty, response.Data?.Html);
        Assert.Equal("https://example.com", response.Metadata?.SourceURL);
        Assert.Equal(200, response.Metadata?.StatusCode);
    }

    [Fact]
    public async Task ScrapeAsync_WithRegularUrlAndEmptyContent_ReturnsErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        
        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ReturnsAsync(default(string));

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlErrorResponse>(result);
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.Contains("An unexpected server error occurred", errorResponse.Error);
        Assert.Contains("Failed to extract content using Jina", errorResponse.Error);
    }

    [Fact]
    public async Task ScrapeAsync_WithTaskCanceledException_ReturnsTimeoutErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        
        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlErrorResponse>(result);
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.Contains("The request timed out after 15 seconds", errorResponse.Error);
        Assert.Contains("https://example.com", errorResponse.Error);
    }

    [Fact]
    public async Task ScrapeAsync_WithHttpRequestException_ReturnsNetworkErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var httpException = new HttpRequestException("Network error");
        
        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ThrowsAsync(httpException);

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlErrorResponse>(result);
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.Contains("Request failed for URL: https://example.com", errorResponse.Error);
        Assert.Contains("Network error", errorResponse.Error);
    }

    [Fact]
    public async Task ScrapeAsync_WithUnexpectedException_ReturnsServerErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var exception = new InvalidOperationException("Unexpected error");
        
        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ThrowsAsync(exception);

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.IsType<FirecrawlErrorResponse>(result);
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.Contains("An unexpected server error occurred", errorResponse.Error);
        Assert.Contains("Unexpected error", errorResponse.Error);
    }

    [Theory]
    [InlineData("https://example.com/test.PDF")]
    [InlineData("https://example.com/test.Pdf")]
    [InlineData("https://example.com/test.pDf")]
    public async Task ScrapeAsync_WithPdfUrlCaseInsensitive_UsesPdfHandler(string url)
    {
        // Arrange
        var request = new ScrapeRequest { Url = url };
        _mockPdfHandler.Setup(x => x.Scrape(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync("PDF content");

        // Act
        await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        _mockPdfHandler.Verify(x => x.Scrape(url, It.IsAny<CancellationToken>()), Times.Once);
        _mockJinaHandler.Verify(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ScrapeAsync_VerifyJinaHandlerCalledWithCorrectParameters()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var authHeader = "Bearer test-token";
        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ReturnsAsync("content");

        // Act
        await _scrapeService.ScrapeAsync(request, authHeader, CancellationToken.None);

        // Assert
        _mockJinaHandler.Verify(x => x.ScrapeWithJina("https://example.com", authHeader, 15), Times.Once);
    }
}