using Scrappy.ConsoleUI;
using Scrappy.Core;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var builder = Host.CreateApplicationBuilder(args);
builder.Services.ConfigureHttpClientDefaults(builder =>
{
    builder.ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");
        // client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate,sdch");
        client.DefaultRequestHeaders.Add("Referrer", "http://google.com");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
    });
});

builder.Services.AddSingleton<ExplosmScraper>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync(cts.Token);