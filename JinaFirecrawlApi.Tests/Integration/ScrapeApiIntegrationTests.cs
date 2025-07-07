using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Tests.Integration;

[TestFixture]
public class ScrapeApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task POST_Scrape_WithoutAuthHeader_Returns401()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/v1/scrape", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task POST_Scrape_WithAuthHeader_ReturnsSuccess()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        // Act
        var response = await _client.PostAsync("/v1/scrape", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var firecrawlResponse = JsonSerializer.Deserialize<FirecrawlResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.That(firecrawlResponse, Is.Not.Null);
        Assert.That(firecrawlResponse!.Success, Is.True);
        Assert.That(firecrawlResponse.Data?.Markdown, Is.EqualTo("Mock content from Jina"));
    }

    [Test]
    public async Task POST_Scrape_WithPdfUrl_UsesPdfHandler()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://example.com/test.pdf" };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        // Act
        var response = await _client.PostAsync("/v1/scrape", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var firecrawlResponse = JsonSerializer.Deserialize<FirecrawlResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.That(firecrawlResponse, Is.Not.Null);
        Assert.That(firecrawlResponse!.Success, Is.True);
        Assert.That(firecrawlResponse.Data?.Markdown, Is.EqualTo("Mock PDF content"));
    }

    [Test]
    public async Task GET_Root_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
    }
}