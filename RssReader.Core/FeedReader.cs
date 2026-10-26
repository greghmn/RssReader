using System.Xml.Linq;

namespace RssReader.Core;

public class FeedReader : IFeedReader
{
    private readonly HttpClient _httpClient;

    public FeedReader()
        : this(new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        })
    {
    }

    public FeedReader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

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
            await _httpClient.GetAsync(
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

        Uri finalFeedUri = response.RequestMessage?.RequestUri ?? feedUri;

        return FeedParser.Parse(document, finalFeedUri);
    }
}
