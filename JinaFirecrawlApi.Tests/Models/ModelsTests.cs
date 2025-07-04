using Xunit;
using JinaFirecrawlApi.Models;

namespace JinaFirecrawlApi.Tests.Models;

public class ModelsTests
{
    [Fact]
    public void ScrapeRequest_CanSetUrl()
    {
        // Arrange
        var request = new ScrapeRequest();
        var url = "https://example.com";

        // Act
        request.Url = url;

        // Assert
        Assert.Equal(url, request.Url);
    }

    [Fact]
    public void FirecrawlResponse_DefaultsSuccessToTrue()
    {
        // Arrange & Act
        var response = new FirecrawlResponse();

        // Assert
        Assert.True(response.Success);
    }

    [Fact]
    public void FirecrawlResponse_CanSetProperties()
    {
        // Arrange
        var response = new FirecrawlResponse();
        var data = new FirecrawlData { Markdown = "test", Html = "<p>test</p>" };
        var metadata = new FirecrawlMetadata { SourceURL = "https://example.com" };

        // Act
        response.Success = false;
        response.Data = data;
        response.Metadata = metadata;

        // Assert
        Assert.False(response.Success);
        Assert.Equal(data, response.Data);
        Assert.Equal(metadata, response.Metadata);
    }

    [Fact]
    public void FirecrawlErrorResponse_CanSetError()
    {
        // Arrange
        var errorResponse = new FirecrawlErrorResponse();
        var errorMessage = "Test error";

        // Act
        errorResponse.Error = errorMessage;

        // Assert
        Assert.Equal(errorMessage, errorResponse.Error);
    }

    [Fact]
    public void FirecrawlData_DefaultsHtmlToEmpty()
    {
        // Arrange & Act
        var data = new FirecrawlData();

        // Assert
        Assert.Equal(string.Empty, data.Html);
    }

    [Fact]
    public void FirecrawlData_CanSetProperties()
    {
        // Arrange
        var data = new FirecrawlData();
        var markdown = "# Test";
        var html = "<h1>Test</h1>";

        // Act
        data.Markdown = markdown;
        data.Html = html;

        // Assert
        Assert.Equal(markdown, data.Markdown);
        Assert.Equal(html, data.Html);
    }

    [Fact]
    public void FirecrawlMetadata_DefaultsToCorrectValues()
    {
        // Arrange & Act
        var metadata = new FirecrawlMetadata();

        // Assert
        Assert.Equal(string.Empty, metadata.Title);
        Assert.Equal(string.Empty, metadata.Description);
        Assert.Equal(string.Empty, metadata.Language);
        Assert.Equal(200, metadata.StatusCode);
    }

    [Fact]
    public void FirecrawlMetadata_CanSetAllProperties()
    {
        // Arrange
        var metadata = new FirecrawlMetadata();
        var sourceUrl = "https://example.com";
        var title = "Test Title";
        var description = "Test Description";
        var language = "en";
        var statusCode = 404;

        // Act
        metadata.SourceURL = sourceUrl;
        metadata.Title = title;
        metadata.Description = description;
        metadata.Language = language;
        metadata.StatusCode = statusCode;

        // Assert
        Assert.Equal(sourceUrl, metadata.SourceURL);
        Assert.Equal(title, metadata.Title);
        Assert.Equal(description, metadata.Description);
        Assert.Equal(language, metadata.Language);
        Assert.Equal(statusCode, metadata.StatusCode);
    }

    [Fact]
    public void FirecrawlResponse_ImplementsIFirecrawlResponseTypes()
    {
        // Arrange & Act
        var response = new FirecrawlResponse();

        // Assert
        Assert.IsAssignableFrom<IFirecrawlResponseTypes>(response);
    }

    [Fact]
    public void FirecrawlErrorResponse_ImplementsIFirecrawlResponseTypes()
    {
        // Arrange & Act
        var errorResponse = new FirecrawlErrorResponse();

        // Assert
        Assert.IsAssignableFrom<IFirecrawlResponseTypes>(errorResponse);
    }
}