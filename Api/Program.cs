using JinaFirecrawlApi.Services;
using JinaFirecrawlApi.Middleware;
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

        builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddScoped<IScrapeService, ScrapeService>();
        builder.Services.AddScoped<IPdfHandler, PdfHandler>();
        builder.Services.AddScoped<IJinaHandler, JinaHandler>();
        builder.Services.AddScoped<IPdfConverter, PdfToTxtFileConverter>();

        var app = builder.Build();

        app.UseForwardedHeaders();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
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
