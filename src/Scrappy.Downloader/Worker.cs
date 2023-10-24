using Scrappy.Core;

namespace Scrappy.Downloader;

public class Worker(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Worker> logger,
        IQueueClient<DownloadRequest> downloadReqQueue,
        IQueueClient<DownloadResult> downloadResultQueue
    ) : BackgroundService
{
    private async Task EnsureQueueIsReady(CancellationToken stoppingToken)
    {
        var i = 0;
        while (downloadReqQueue.QueueExists() is false && (++i < 60))
        {
            logger.LogInformation("Waiting for transaction queue to be created...");
            await Task.Delay(1000, stoppingToken);
        }

        if (i is 60)
        {
            throw new TimeoutException(
                "Ensure TxnQueue timed out after 60 seconds"
            );
        }

        logger.LogInformation("Txn Queue is Ready and Reachable");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureQueueIsReady(stoppingToken);
        while (stoppingToken.IsCancellationRequested is false)
        {
            try
            {
                downloadReqQueue.Consume(
                    async (downloadRequest, ctx) =>
                    {
                        logger.LogInformation("Received download request for {Uri}", downloadRequest.Uri);
                        await DownloadFileAsync(downloadRequest, ctx);
                    },
                    stoppingToken
                );
            }
            catch (Exception exn)
            {
                logger.LogError(exn, "Error occurred while consuming message. Trying again");
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    private async Task DownloadFileAsync(DownloadRequest req, CancellationToken ctx)
    {
        var downloadPath = configuration.GetValue<string>("DownloadPath");
        if (string.IsNullOrWhiteSpace(downloadPath))
        {
            downloadPath = "/var/scrappy/downloads";
        }

        if (Path.Exists(downloadPath) is false)
        {
            Directory.CreateDirectory(downloadPath);
        }

        var fileName = Path.GetFileName(req.Uri.AbsolutePath);
        var filePath = Path.Combine(downloadPath, fileName);

        using var response =
            await httpClient.GetAsync(
                req.Uri,
                HttpCompletionOption.ResponseHeadersRead,
                ctx
            );

        response.EnsureSuccessStatusCode();

        await using var contentStream =
            await response.Content.ReadAsStreamAsync(ctx);

        await using var fileStream =
            new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                8192,
                true
            );

        await contentStream.CopyToAsync(fileStream, ctx);
        fileStream.Close();
        contentStream.Close();
        logger.LogInformation("Downloaded {FileName} to {FilePath}", fileName, filePath);
    }
}
