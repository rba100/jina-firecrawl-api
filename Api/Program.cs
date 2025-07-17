using JinaFirecrawlApi.Services;
using JinaFirecrawlApi.Middleware;
using JinaFirecrawlApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

namespace JinaFirecrawlApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        builder.Services.AddHttpContextAccessor();

        builder.Services.Configure<ScrapeOptions>(builder.Configuration.GetSection("Scrape"));
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped<IScrapeService, ScrapeService>();
        builder.Services.AddScoped<IPdfHandler, PdfHandler>();
        builder.Services.AddScoped<IJinaHandler, JinaHandler>();
        builder.Services.AddScoped<IPdfConverter, PdfToTxtFileConverter>();

        var app = builder.Build();

        // Log configured settings at startup
        var scrapeOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<JinaFirecrawlApi.Models.ScrapeOptions>>().Value;
        var logger = app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("Startup");
        logger.LogInformation("Configured Scrape TimeoutSeconds: {TimeoutSeconds}", scrapeOptions.TimeoutSeconds);

        app.UseForwardedHeaders();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseMiddleware<ApiKeyMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
