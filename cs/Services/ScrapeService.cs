using System.Runtime.CompilerServices;
using JinaFirecrawlApi.Models;

namespace JinaFirecrawlApi.Services;

public class ScrapeService : IScrapeService
{
    private const int DefaultTimeoutSeconds = 15;
    private readonly IPdfHandler _pdfHandler;
    private readonly IJinaHandler _jinaHandler;
    private readonly ILogger<ScrapeService> _logger;

    public ScrapeService(IPdfHandler pdfHandler, IJinaHandler jinaHandler, ILogger<ScrapeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        _pdfHandler = pdfHandler ?? throw new ArgumentNullException(nameof(pdfHandler), "PDF handler cannot be null.");
        _jinaHandler = jinaHandler ?? throw new ArgumentNullException(nameof(jinaHandler), "Jina handler cannot be null.");
    }

    public async Task<IFirecrawlResponseTypes> ScrapeAsync(ScrapeRequest request, string authHeader, CancellationToken cancellationToken)
    {
        var sourceUrl = request.Url ?? throw new ArgumentNullException(nameof(request.Url), "URL cannot be null or empty.");
        string? markdownContent = null;
        int statusCode = 200;
        string title = sourceUrl;
        string description = "Scraped content";
        string language = "en";

        try
        {
            if (sourceUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (!sourceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Invalid URL scheme for PDF. Must be http or https.");
                }
                markdownContent = await _pdfHandler.Scrape(sourceUrl, cancellationToken);
                if (string.IsNullOrWhiteSpace(markdownContent))
                {
                    throw new Exception("Failed to extract content from PDF.");
                }
            }
            else
            {
                markdownContent = await _jinaHandler.ScrapeWithJina(sourceUrl, authHeader, DefaultTimeoutSeconds);
                if (string.IsNullOrWhiteSpace(markdownContent))
                {
                    throw new Exception("Failed to extract content using Jina.");
                }
            }
        }
        catch (TaskCanceledException)
        {
            statusCode = 504;
            _logger.LogWarning("Request timed out while scraping URL: {Url}", sourceUrl);
            return CreateErrorResponse($"The request timed out after {DefaultTimeoutSeconds} seconds while scraping URL: {sourceUrl}");
        }
        catch (HttpRequestException ex)
        {
            statusCode = 503;
            throw new Exception($"Request failed for URL: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            statusCode = 400;
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            statusCode = statusCode == 200 ? 500 : statusCode;
            _logger.LogError(ex, "An unexpected error occurred while scraping URL: {Url}", sourceUrl);
            return CreateErrorResponse($"An unexpected server error occurred: {ex.Message}");
        }

        return new FirecrawlResponse
        {
            Success = true,
            Data = new FirecrawlData { Markdown = markdownContent, Html = string.Empty },
            Metadata = new FirecrawlMetadata
            {
                SourceURL = sourceUrl,
                Title = title,
                Description = description,
                Language = language,
                StatusCode = statusCode
            }
        };
    }

    private static FirecrawlErrorResponse CreateErrorResponse(string errorMessage)
    {
        return new FirecrawlErrorResponse
        {
            Error = errorMessage
        };
    }
}
