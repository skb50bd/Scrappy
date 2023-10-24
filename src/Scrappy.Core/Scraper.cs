using Microsoft.Extensions.Logging;

namespace Scrappy.Core;

public abstract class Scraper(ILogger<Scraper> logger, HttpClient httpClient, Uri baseUrl)
{
    protected readonly Uri BaseUrl = baseUrl;
    protected readonly HttpClient HttpClient = httpClient;
    public abstract Task<List<Page>> GetPagesAsync(
        int take,
        CancellationToken cancellationToken
    );

    public abstract Task<Page> GetPage(
        Uri url,
        CancellationToken cancellationToken
    );

    public abstract Task<Page?> GetNextPage(
        Page page,
        CancellationToken cancellationToken
    );

    public async Task<string> GetHtmlAsString(Uri url, CancellationToken cancellationToken)
    {
        using var response =
            await HttpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return html;
    }

    public abstract List<Uri> GetContentUrls(Page page);
}