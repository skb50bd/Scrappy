using Scrappy.Core;

namespace Scrappy.ConsoleUI;

public class Worker(
        IConfiguration configuration,
        ILogger<Worker> logger,
        ExplosmScraper scraper
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Page> pages;
        do
        {
            pages = await scraper.GetPagesAsync(10, stoppingToken);
            foreach (var page in pages)
            {
                logger.LogInformation("Downloading content from {Url}", page.Url);
                await Task.Delay(1000, stoppingToken);
            }
        } while (pages.Count is 10);
    }
}