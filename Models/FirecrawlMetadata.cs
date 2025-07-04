namespace JinaFirecrawlApi.Models;

public class FirecrawlMetadata
{
    public string? SourceURL { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
}
