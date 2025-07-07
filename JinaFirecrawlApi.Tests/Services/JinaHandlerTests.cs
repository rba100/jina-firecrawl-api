using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Services;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

public class JinaHandlerTests
{
    private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private JinaHandler _jinaHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(nameof(JinaHandler)))
                             .Returns(_httpClient);
        
        _jinaHandler = new JinaHandler(_mockHttpClientFactory.Object);
    }

    [Test]
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
        Assert.That(result, Is.EqualTo(expectedContent));
    }

    [Test]
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
        Assert.That(_httpClient.Timeout, Is.EqualTo(TimeSpan.FromSeconds(timeoutSeconds)));
        Assert.That(_httpClient.DefaultRequestHeaders.Authorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(_httpClient.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo("test-token"));
    }

    [Test]
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
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(capturedRequest.RequestUri?.ToString(), Is.EqualTo("https://r.jina.ai/"));
        Assert.That(capturedRequest.Content?.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(capturedRequest.Content?.Headers.ContentType?.CharSet, Is.EqualTo("utf-8"));
    }

    [Test]
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
        Assert.That(_httpClient.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo("test-token"));
    }

    [Test]
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
        Assert.That(_httpClient.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo("test-token"));
    }

    [Test]
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
        Assert.ThrowsAsync<HttpRequestException>(async () => 
            await _jinaHandler.ScrapeWithJina(url, authHeader, 30));
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }
}