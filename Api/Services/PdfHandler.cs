namespace JinaFirecrawlApi.Services;

public class PdfHandler : IPdfHandler
{
    private readonly IPdfConverter _pdfConverter;
    private readonly IHttpClientFactory _httpClientFactory;

    public PdfHandler(IPdfConverter pdfConverter, IHttpClientFactory httpClientFactory)
    {
        _pdfConverter = pdfConverter ?? throw new ArgumentNullException(nameof(pdfConverter));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<string> Scrape(string url, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var outputStream = new MemoryStream();
        using var requestStream = await response.Content.ReadAsStreamAsync();
        using var inputStream = new MemoryStream();
        await requestStream.CopyToAsync(inputStream, cancellationToken);
        inputStream.Position = 0;
        await _pdfConverter.ConvertAsync(inputStream, outputStream, cancellationToken);
        outputStream.Position = 0;
        using var reader = new StreamReader(outputStream);
        var markdownContent = await reader.ReadToEndAsync();
        return markdownContent;
    }
}