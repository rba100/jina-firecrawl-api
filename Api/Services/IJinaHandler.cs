using System.Threading.Tasks;

namespace JinaFirecrawlApi.Services;

public interface IJinaHandler
{
    Task<string> ScrapeWithJina(string url, string authHeader, int timeoutSeconds);
}
