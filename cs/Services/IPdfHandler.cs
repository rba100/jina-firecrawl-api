using System.Threading.Tasks;

namespace JinaFirecrawlApi.Services;

public interface IPdfHandler
{
    Task<string> Scrape(string url, CancellationToken cancellationToken);
}
