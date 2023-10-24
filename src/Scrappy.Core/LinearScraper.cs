using Microsoft.Extensions.Logging;

namespace Scrappy.Core;

public abstract class LinearScraper(
        ILogger<LinearScraper> logger,
        HttpClient httpClient,
        Uri baseUrl
    ) : Scraper(logger, httpClient, baseUrl)
{
    protected abstract Uri? GetNextPageUri(Page page);

    public override async Task<List<Page>> GetPagesAsync(
        int take,
        CancellationToken cancellationToken
    )
    {
        Uri? nextPageUrl = BaseUrl;
        List<Page> pages = [];
        while (pages.Count < take
            && nextPageUrl is not null
            && cancellationToken.IsCancellationRequested is false
        )
        {
            var page = new Page { Url = nextPageUrl };
            var html = await GetHtmlAsString(page.Url, cancellationToken);
            page.PageContent = html;
            pages.Add(page);
            nextPageUrl = GetNextPageUri(page);
        }

        return pages;
    }
}
