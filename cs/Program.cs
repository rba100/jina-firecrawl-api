using JinaFirecrawlApi.Services;
using JinaFirecrawlApi.Middleware;

namespace JinaFirecrawlApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped<IScrapeService, ScrapeService>();
        builder.Services.AddScoped<IPdfHandler, PdfHandler>();
        builder.Services.AddScoped<IJinaHandler, JinaHandler>();
        builder.Services.AddScoped<IPdfConverter, PdfToTxtFileConverter>();

        var app = builder.Build();

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
