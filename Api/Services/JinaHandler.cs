using System.Net.Http.Headers;
using System.Text.Json;

namespace JinaFirecrawlApi.Services;

public class JinaHandler : IJinaHandler
{
    private readonly IHttpClientFactory _httpClientFactory;

    public JinaHandler(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> ScrapeWithJina(string url, string authHeader, int timeoutSeconds)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(JinaHandler));
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authHeader.Replace("Bearer ", ""));
        var fallbackTimeout = timeoutSeconds - 4;
        if (fallbackTimeout > 0) httpClient.DefaultRequestHeaders.Add("X-Timeout", fallbackTimeout.ToString());

        var payload = new { url = url };
        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync("https://r.jina.ai", content);

        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        return responseString;
    }
}
