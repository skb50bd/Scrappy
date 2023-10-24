using Scrappy.Core;

namespace Scrappy.Crawler;

public class Worker(
        IConfiguration configuration,
        ILogger<Worker> logger,
        IQueueClient<DownloadRequest> dlReqQueue,
        ExplosmScraper scraper
    ) : BackgroundService
{

    private async Task EnsureQueueIsReady(CancellationToken stoppingToken)
    {
        var i = 0;
        while (dlReqQueue.EnsureQueueExists() is false && (++i < 60))
        {
            logger.LogInformation("Waiting for queue to be created...");
            await Task.Delay(1000, stoppingToken);
        }

        if (i is 60)
        {
            throw new TimeoutException(
                "Ensure Queue timed out after 60 seconds"
            );
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await EnsureQueueIsReady(stoppingToken);
        }
        catch (TimeoutException exn)
        {
            logger.LogError(exn , "Ensure Queue timed out after 60 seconds");
            return;
        }
        catch (Exception exn)
        {
            logger.LogCritical(exn, "Unknown error occurred while ensuring queue");
            return;
        }

        var visitedPages = new HashSet<string>();
        var nextPage =
            await scraper.GetPage(
                new Uri("https://explosm.net/"),
                stoppingToken
            );

        while (nextPage is not null
            && stoppingToken.IsCancellationRequested is false
        )
        {
            try
            {
                var contentUrls = scraper.GetContentUrls(nextPage);
                if (contentUrls.Count is 0)
                {
                    break;
                }

                foreach (var contentUrl in contentUrls)
                {
                    dlReqQueue.Publish(
                        new DownloadRequest
                        {
                            Uri = contentUrl
                        }
                    );
                }

                visitedPages.Add(nextPage.Url.ToString());
                nextPage = await scraper.GetNextPage(nextPage, stoppingToken);
                if (nextPage is not null
                    && visitedPages.Contains(nextPage.Url.ToString())
                )
                {
                    logger.LogWarning("Loop Detected");
                    break;
                }
            }
            catch (Exception exn)
            {
                logger.LogError(exn, "Error occurred while publishing message. Trying again");
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}