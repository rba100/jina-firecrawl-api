namespace JinaFirecrawlApi.Models;

public class FirecrawlResponse : IFirecrawlResponseTypes
{
    public bool Success { get; set; } = true;
    public FirecrawlData? Data { get; set; }
    public FirecrawlMetadata? Metadata { get; set; }
}

public interface IFirecrawlResponseTypes
{

}