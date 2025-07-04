namespace JinaFirecrawlApi.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string API_KEY_HEADER = "Authorization";

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // root path is health check, no API key required
        if (context.Request.Path == "/")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.ContainsKey(API_KEY_HEADER))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authorization header required" });
            return;
        }

        await _next(context);
    }
}
