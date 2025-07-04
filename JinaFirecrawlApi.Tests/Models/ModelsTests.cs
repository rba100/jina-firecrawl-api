using JinaFirecrawlApi.Models;

namespace JinaFirecrawlApi.Tests.Models;

public class ModelsTests
{
    [Test]
    public void ScrapeRequest_CanSetUrl()
    {
        // Arrange
        var request = new ScrapeRequest();
        var url = "https://example.com";

        // Act
        request.Url = url;

        // Assert
        Assert.That(request.Url, Is.EqualTo(url));
    }

    [Test]
    public void FirecrawlResponse_DefaultsSuccessToTrue()
    {
        // Arrange & Act
        var response = new FirecrawlResponse();

        // Assert
        Assert.That(response.Success, Is.True);
    }

    [Test]
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
        Assert.That(response.Success, Is.False);
        Assert.That(response.Data, Is.EqualTo(data));
        Assert.That(response.Metadata, Is.EqualTo(metadata));
    }

    [Test]
    public void FirecrawlErrorResponse_CanSetError()
    {
        // Arrange
        var errorResponse = new FirecrawlErrorResponse();
        var errorMessage = "Test error";

        // Act
        errorResponse.Error = errorMessage;

        // Assert
        Assert.That(errorResponse.Error, Is.EqualTo(errorMessage));
    }

    [Test]
    public void FirecrawlData_DefaultsHtmlToEmpty()
    {
        // Arrange & Act
        var data = new FirecrawlData();

        // Assert
        Assert.That(data.Html, Is.EqualTo(string.Empty));
    }

    [Test]
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
        Assert.That(data.Markdown, Is.EqualTo(markdown));
        Assert.That(data.Html, Is.EqualTo(html));
    }

    [Test]
    public void FirecrawlMetadata_DefaultsToCorrectValues()
    {
        // Arrange & Act
        var metadata = new FirecrawlMetadata();

        // Assert
        Assert.That(metadata.Title, Is.EqualTo(string.Empty));
        Assert.That(metadata.Description, Is.EqualTo(string.Empty));
        Assert.That(metadata.Language, Is.EqualTo(string.Empty));
        Assert.That(metadata.StatusCode, Is.EqualTo(200));
    }

    [Test]
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
        Assert.That(metadata.SourceURL, Is.EqualTo(sourceUrl));
        Assert.That(metadata.Title, Is.EqualTo(title));
        Assert.That(metadata.Description, Is.EqualTo(description));
        Assert.That(metadata.Language, Is.EqualTo(language));
        Assert.That(metadata.StatusCode, Is.EqualTo(statusCode));
    }

    [Test]
    public void FirecrawlResponse_ImplementsIFirecrawlResponseTypes()
    {
        // Arrange & Act
        var response = new FirecrawlResponse();

        // Assert
        Assert.That(response, Is.InstanceOf<IFirecrawlResponseTypes>());
    }

    [Test]
    public void FirecrawlErrorResponse_ImplementsIFirecrawlResponseTypes()
    {
        // Arrange & Act
        var errorResponse = new FirecrawlErrorResponse();

        // Assert
        Assert.That(errorResponse, Is.InstanceOf<IFirecrawlResponseTypes>());
    }
}