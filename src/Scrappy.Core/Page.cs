using HtmlAgilityPack;

namespace Scrappy.Core;

public static class Topics
{
    public const string DownloadRequest = "download_request";
    public const string DownloadResult = "download_result";
}

public class DownloadRequest
{
    public required Uri Uri { get; init; }
}

public class DownloadResult
{
    public Uri? Uri { get; init; }
    public string? FileName { get; init; }
    public string? Content { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

public class Page
{
    public required Uri Url { get; init; }

    private string? _pageContent;
    public string? PageContent
    {
        get => _pageContent;
        set
        {
            _pageContent = value;
            HtmlDocument = new HtmlDocument();
            HtmlDocument.LoadHtml(_pageContent);
        }
    }

    public HtmlDocument? HtmlDocument { get; private set; }

    public async Task<string?> Load(Func<Uri, CancellationToken, Task<string>> htmlGetter, CancellationToken cancellationToken = default)
    {
        if (PageContent is null)
        {
            PageContent = await htmlGetter(Url, cancellationToken);
            HtmlDocument = new HtmlDocument();
            HtmlDocument.LoadHtml(PageContent);
        }

        return PageContent;
    }
}
