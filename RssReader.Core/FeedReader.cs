
using System.Xml.Linq;

namespace RssReader.Core;

public class FeedReader
{
    private static readonly HttpClient HttpClient = new();

    public async Task<List<FeedItem>> ReadAsync(string feedUrl)
    {
        using HttpResponseMessage response =
            await HttpClient.GetAsync(feedUrl);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync();

        XDocument document = await XDocument.LoadAsync(
            stream,
            LoadOptions.None,
            CancellationToken.None);

        return FeedParser.Parse(document);
    }
}
