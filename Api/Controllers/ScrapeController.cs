using Microsoft.AspNetCore.Mvc;
using JinaFirecrawlApi.Models;
using JinaFirecrawlApi.Services;

namespace JinaFirecrawlApi.Controllers;

[ApiController]
[Route("v1")]
public class ScrapeController : ControllerBase
{
    private readonly IScrapeService _scrapeService;

    public ScrapeController(IScrapeService scrapeService)
    {
        _scrapeService = scrapeService;
    }

    [HttpPost("scrape")]
    [ProducesResponseType(typeof(FirecrawlResponse), 200)]
    [ProducesResponseType(typeof(FirecrawlErrorResponse), 400)]
    [ProducesResponseType(typeof(FirecrawlErrorResponse), 500)]
    [ProducesResponseType(typeof(FirecrawlErrorResponse), 503)]
    public async Task<IActionResult> Scrape([FromBody] ScrapeRequest request, CancellationToken cancellationToken)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            return Unauthorized(new FirecrawlErrorResponse { Error = "Authorization header required" });
        }
        try
        {
            var result = await _scrapeService.ScrapeAsync(request, authHeader, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new FirecrawlErrorResponse { Error = $"Request failed for URL: {ex.Message}" });
        }
    }
}
