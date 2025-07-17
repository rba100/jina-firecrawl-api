using System.Runtime.CompilerServices;
using JinaFirecrawlApi.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace JinaFirecrawlApi.Services;

public class ScrapeService : IScrapeService
{
    private readonly int _timeoutSeconds;
    private readonly IPdfHandler _pdfHandler;
    private readonly IJinaHandler _jinaHandler;
    private readonly ILogger<ScrapeService> _logger;

    public ScrapeService(IPdfHandler pdfHandler, IJinaHandler jinaHandler, ILogger<ScrapeService> logger, IOptions<ScrapeOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        _pdfHandler = pdfHandler ?? throw new ArgumentNullException(nameof(pdfHandler), "PDF handler cannot be null.");
        _jinaHandler = jinaHandler ?? throw new ArgumentNullException(nameof(jinaHandler), "Jina handler cannot be null.");
        if (options?.Value?.TimeoutSeconds is int t && t > 0)
            _timeoutSeconds = t;
        else
            _timeoutSeconds = 15;
    }

    public async Task<IFirecrawlResponseTypes> ScrapeAsync(ScrapeRequest request, string authHeader, CancellationToken cancellationToken)
    {
        var sourceUrl = request.Url ?? throw new ArgumentNullException(nameof(request.Url), "URL cannot be null or empty.");
        int statusCode = 200;
        string title = sourceUrl;
        string description = "Scraped content";
        string language = "en";
        var sw = new Stopwatch();
        sw.Start();

        string? markdownContent;
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
                markdownContent = await _jinaHandler.ScrapeWithJina(sourceUrl, authHeader, _timeoutSeconds);
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
            return CreateErrorResponse($"The request timed out after {_timeoutSeconds} seconds while scraping URL: {sourceUrl}");
        }
        catch (HttpRequestException ex)
        {
            statusCode = 503;
            _logger.LogError(ex, "Request failed for URL: {Url}", sourceUrl);
            return CreateErrorResponse($"Request failed for URL: {sourceUrl}. Error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            statusCode = 400;
            return CreateErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            statusCode = statusCode == 200 ? 500 : statusCode;
            _logger.LogError(ex, "An unexpected error occurred while scraping URL: {Url}", sourceUrl);
            return CreateErrorResponse($"An unexpected server error occurred: {ex.Message}");
        }

        sw.Stop();
        _logger.LogInformation("Successfully scraped URL: {Url} in {ElapsedMilliseconds} ms", sourceUrl, sw.ElapsedMilliseconds);

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
