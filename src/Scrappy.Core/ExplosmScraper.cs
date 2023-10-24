using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Logging;

namespace Scrappy.Core;

public sealed class ExplosmScraper(ILogger<ExplosmScraper> logger, HttpClient httpClient)
    : LinearScraper(logger, httpClient, new Uri("https://explosm.net/"))
{
    private HtmlNode GetFirstNodeWithClassPrefixMatch(Page page, string classPrefix)
    {
        if (page.HtmlDocument is null)
        {
            throw new InvalidOperationException(
                "Page must be loaded before getting the next page URI."
            );
        }

        try
        {
            var className =
                page.HtmlDocument.DocumentNode.DescendantsAndSelf()
                    .Where(n => n.Attributes.Contains("class"))
                    .SelectMany(n => n.Attributes["class"].Value.Split())
                    .Distinct()
                    .Single(n => n.StartsWith(classPrefix));

            return
                page.HtmlDocument.DocumentNode
                    .QuerySelectorAll($"div.{className}").First();
        }
        catch (Exception exn)
        {
            throw new InvalidOperationException(
                $"Could not find node with class prefix {classPrefix}. \n"
                + $"Page URL: {page.Url}, Content: {page.PageContent}",
                exn
            );
        }
    }

    public override List<Uri> GetContentUrls(Page page)
    {
        var comicImageUri =
            GetFirstNodeWithClassPrefixMatch(page, "MainComic__ComicImage")
                .FirstChild
                .FirstChild
                .Attributes["src"].Value;

        if (string.IsNullOrWhiteSpace(comicImageUri))
        {
            throw new InvalidOperationException(
                "Could not find comic image URI."
            );
        }

        return [new Uri(comicImageUri)];
    }

    public override async Task<Page> GetPage(Uri url, CancellationToken cancellationToken)
    {
        var html = await GetHtmlAsString(url, cancellationToken);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        return new Page
        {
            Url         = url,
            PageContent = html
        };
    }

    public override async Task<Page?> GetNextPage(Page page, CancellationToken cancellationToken)
    {
        var nextUri = GetNextPageUri(page);

        if (nextUri is null)
        {
            return null;
        }

        return await GetPage(nextUri, cancellationToken);
    }

    protected override Uri? GetNextPageUri(Page page)
    {
        var nodeWeWant = GetFirstNodeWithClassPrefixMatch(page, "ComicSelector__Container");
        var nextComicUri =
            nodeWeWant
                .ChildNodes.Where(c => c.InnerHtml.Contains("rotate=\"180deg\""))
                .First()
                .Attributes["href"].Value;

        if (string.IsNullOrWhiteSpace(nextComicUri))
        {
            return null;
        }

        var uriBuilder = new UriBuilder(BaseUrl)
        {
            Path = nextComicUri.Replace("#comic", "")
        };

        logger.LogInformation("Page: {pageUri} Next page URI: {nextPageUri}", page.Url, uriBuilder.Uri);

        return uriBuilder.Uri;
    }
}
