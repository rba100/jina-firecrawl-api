using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Services;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

public class ScrapeServiceTests
{
    private Mock<IPdfHandler> _mockPdfHandler = null!;
    private Mock<IJinaHandler> _mockJinaHandler = null!;
    private Mock<ILogger<ScrapeService>> _mockLogger = null!;
    private Mock<Microsoft.Extensions.Options.IOptions<ScrapeOptions>> _mockOptions = null!;
    private ScrapeService _scrapeService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPdfHandler = new Mock<IPdfHandler>();
        _mockJinaHandler = new Mock<IJinaHandler>();
        _mockLogger = new Mock<ILogger<ScrapeService>>();
        _mockOptions = new Mock<Microsoft.Extensions.Options.IOptions<ScrapeOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new ScrapeOptions { TimeoutSeconds = 15 });
        _scrapeService = new ScrapeService(_mockPdfHandler.Object, _mockJinaHandler.Object, _mockLogger.Object, _mockOptions.Object);
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ScrapeService(_mockPdfHandler.Object, _mockJinaHandler.Object, null!, _mockOptions.Object));
        Assert.That(exception.ParamName, Is.EqualTo("logger"));
        Assert.That(exception.Message, Does.Contain("Logger cannot be null"));
    }

    [Test]
    public void Constructor_WithNullPdfHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ScrapeService(null!, _mockJinaHandler.Object, _mockLogger.Object, _mockOptions.Object));
        Assert.That(exception.ParamName, Is.EqualTo("pdfHandler"));
        Assert.That(exception.Message, Does.Contain("PDF handler cannot be null"));
    }

    [Test]
    public void Constructor_WithNullJinaHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ScrapeService(_mockPdfHandler.Object, null!, _mockLogger.Object, _mockOptions.Object));
        Assert.That(exception.ParamName, Is.EqualTo("jinaHandler"));
        Assert.That(exception.Message, Does.Contain("Jina handler cannot be null"));
    }

    [Test]
    public async Task ScrapeAsync_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new ScrapeRequest { Url = null };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None));
        Assert.That(exception.Message, Does.Contain("URL cannot be null or empty"));
    }

    [Test]
    public async Task ScrapeAsync_WithPdfUrlAndInvalidScheme_ReturnsErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "file://test.pdf" };

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<FirecrawlErrorResponse>());
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.That(errorResponse.Error, Is.EqualTo("Invalid URL scheme for PDF. Must be http or https."));
    }

    [Test]
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
        Assert.That(result, Is.TypeOf<FirecrawlResponse>());
        var response = (FirecrawlResponse)result;
        Assert.That(response.Success, Is.True);
        Assert.That(response.Data?.Markdown, Is.EqualTo(expectedMarkdown));
        Assert.That(response.Data?.Html, Is.EqualTo(string.Empty));
        Assert.That(response.Metadata?.SourceURL, Is.EqualTo("https://example.com/test.pdf"));
        Assert.That(response.Metadata?.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task ScrapeAsync_WithPdfUrlAndEmptyContent_ReturnsErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com/test.pdf" };

        _mockPdfHandler.Setup(x => x.Scrape(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(string.Empty);

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<FirecrawlErrorResponse>());
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.That(errorResponse.Error, Does.Contain("An unexpected server error occurred"));
        Assert.That(errorResponse.Error, Does.Contain("Failed to extract content from PDF"));
    }

    [Test]
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
        Assert.That(result, Is.TypeOf<FirecrawlResponse>());
        var response = (FirecrawlResponse)result;
        Assert.That(response.Success, Is.True);
        Assert.That(response.Data?.Markdown, Is.EqualTo(expectedMarkdown));
        Assert.That(response.Data?.Html, Is.EqualTo(string.Empty));
        Assert.That(response.Metadata?.SourceURL, Is.EqualTo("https://example.com"));
        Assert.That(response.Metadata?.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task ScrapeAsync_WithRegularUrlAndEmptyContent_ReturnsErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };

        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ReturnsAsync(string.Empty);
        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<FirecrawlErrorResponse>());
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.That(errorResponse.Error, Does.Contain("An unexpected server error occurred"));
        Assert.That(errorResponse.Error, Does.Contain("Failed to extract content using Jina"));
    }

    [Test]
    public async Task ScrapeAsync_WithTaskCanceledException_ReturnsTimeoutErrorResponse()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };

        _mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                       .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await _scrapeService.ScrapeAsync(request, "Bearer token", CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<FirecrawlErrorResponse>());
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.That(errorResponse.Error, Does.Contain("The request timed out after 15 seconds"));
        Assert.That(errorResponse.Error, Does.Contain("https://example.com"));
    }

    [Test]
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
        Assert.That(result, Is.TypeOf<FirecrawlErrorResponse>());
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.That(errorResponse.Error, Does.Contain("Request failed for URL: https://example.com"));
        Assert.That(errorResponse.Error, Does.Contain("Network error"));
    }

    [Test]
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
        Assert.That(result, Is.TypeOf<FirecrawlErrorResponse>());
        var errorResponse = (FirecrawlErrorResponse)result;
        Assert.That(errorResponse.Error, Does.Contain("An unexpected server error occurred"));
        Assert.That(errorResponse.Error, Does.Contain("Unexpected error"));
    }

    [Test]
    [TestCase("https://example.com/test.PDF")]
    [TestCase("https://example.com/test.Pdf")]
    [TestCase("https://example.com/test.pDf")]
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

    [Test]
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
