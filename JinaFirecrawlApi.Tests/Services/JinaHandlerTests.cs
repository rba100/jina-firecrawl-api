using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Services;

public class JinaHandlerTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly JinaHandler _jinaHandler;

    public JinaHandlerTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(nameof(JinaHandler)))
                             .Returns(_httpClient);
        
        _jinaHandler = new JinaHandler(_mockHttpClientFactory.Object);
    }

    [Fact]
    public async Task ScrapeWithJina_WithValidResponse_ReturnsContent()
    {
        // Arrange
        var url = "https://example.com";
        var authHeader = "Bearer test-token";
        var expectedContent = "scraped content";
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _jinaHandler.ScrapeWithJina(url, authHeader, 30);

        // Assert
        Assert.Equal(expectedContent, result);
    }

    [Fact]
    public async Task ScrapeWithJina_ConfiguresHttpClientCorrectly()
    {
        // Arrange
        var url = "https://example.com";
        var authHeader = "Bearer test-token";
        var timeoutSeconds = 25;
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("content")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        await _jinaHandler.ScrapeWithJina(url, authHeader, timeoutSeconds);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(timeoutSeconds), _httpClient.Timeout);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("test-token", _httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task ScrapeWithJina_SendsCorrectPostRequest()
    {
        // Arrange
        var url = "https://example.com";
        var authHeader = "Bearer test-token";
        HttpRequestMessage? capturedRequest = null;
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("content")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        // Act
        await _jinaHandler.ScrapeWithJina(url, authHeader, 30);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://r.jina.ai/", capturedRequest.RequestUri?.ToString());
        Assert.Equal("application/json", capturedRequest.Content?.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", capturedRequest.Content?.Headers.ContentType?.CharSet);
    }

    [Fact]
    public async Task ScrapeWithJina_WithBearerPrefix_StripsPrefix()
    {
        // Arrange
        var url = "https://example.com";
        var authHeader = "Bearer test-token";
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("content")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        await _jinaHandler.ScrapeWithJina(url, authHeader, 30);

        // Assert
        Assert.Equal("test-token", _httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task ScrapeWithJina_WithoutBearerPrefix_UsesTokenAsIs()
    {
        // Arrange
        var url = "https://example.com";
        var authHeader = "test-token";
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("content")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        await _jinaHandler.ScrapeWithJina(url, authHeader, 30);

        // Assert
        Assert.Equal("test-token", _httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task ScrapeWithJina_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var url = "https://example.com";
        var authHeader = "Bearer test-token";
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _jinaHandler.ScrapeWithJina(url, authHeader, 30));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}