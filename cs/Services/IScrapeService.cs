using System.Threading.Tasks;
using JinaFirecrawlApi.Models;

namespace JinaFirecrawlApi.Services;

public interface IScrapeService
{
    Task<IFirecrawlResponseTypes> ScrapeAsync(ScrapeRequest request, string authHeader, CancellationToken cancellationToken);
}
