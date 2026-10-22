
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
        if (!Uri.TryCreate(
        feedUrl,
        UriKind.Absolute,
        out Uri? feedUri)
    || (feedUri.Scheme != Uri.UriSchemeHttp
        && feedUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "The feed URL must be an absolute HTTP or HTTPS URL.",
                nameof(feedUrl));
        }
        using HttpResponseMessage response =
            await HttpClient.GetAsync(
                feedUri,
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
