using JinaFirecrawlApi.Middleware;

namespace JinaFirecrawlApi.Extensions;

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
