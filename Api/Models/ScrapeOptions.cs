namespace JinaFirecrawlApi.Models
{
    public class ScrapeOptions
    {
        /// <summary>
        /// Timeout in seconds for scraping operations. Can be set via appsettings or the SCRAPE__TIMEOUTSECONDS environment variable (double underscore).
        /// </summary>
        public int TimeoutSeconds { get; set; } = 15;
    }
}
