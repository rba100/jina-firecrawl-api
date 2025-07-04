using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Integration;

public class ScrapeApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ScrapeApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with mocks for integration testing
                var mockJinaHandler = new Mock<IJinaHandler>();
                mockJinaHandler.Setup(x => x.ScrapeWithJina(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                              .ReturnsAsync("Mock content from Jina");

                var mockPdfHandler = new Mock<IPdfHandler>();
                mockPdfHandler.Setup(x => x.Scrape(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                             .ReturnsAsync("Mock PDF content");

                // Remove the real services and add mocks
                services.AddScoped(_ => mockJinaHandler.Object);
                services.AddScoped(_ => mockPdfHandler.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_Scrape_WithoutAuthHeader_Returns401()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1/scrape", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_Scrape_WithAuthHeader_ReturnsSuccess()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        // Act
        var response = await _client.PostAsync("/v1/scrape", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var firecrawlResponse = JsonSerializer.Deserialize<FirecrawlResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(firecrawlResponse);
        Assert.True(firecrawlResponse.Success);
        Assert.Equal("Mock content from Jina", firecrawlResponse.Data?.Markdown);
    }

    [Fact]
    public async Task POST_Scrape_WithPdfUrl_UsesPdfHandler()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com/test.pdf" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        // Act
        var response = await _client.PostAsync("/v1/scrape", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var firecrawlResponse = JsonSerializer.Deserialize<FirecrawlResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(firecrawlResponse);
        Assert.True(firecrawlResponse.Success);
        Assert.Equal("Mock PDF content", firecrawlResponse.Data?.Markdown);
    }

    [Fact]
    public async Task GET_Root_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}