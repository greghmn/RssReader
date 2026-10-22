
using System.Xml.Linq;

namespace RssReader.Core;

public class FeedReader : IFeedReader
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<List<FeedItem>> ReadAsync(
        string feedUrl,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await HttpClient.GetAsync(
                feedUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        XDocument document = await XDocument.LoadAsync(
            stream,
            LoadOptions.None,
            cancellationToken);

        return FeedParser.Parse(document);
    }
}
