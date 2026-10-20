
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

        List<FeedItem> items = [];

        foreach (XElement element in document.Descendants("item"))
        {
            FeedItem item = new()
            {
                Title = (string?)element.Element("title") ?? "",
                Link = (string?)element.Element("link") ?? "",
                Description =
                    (string?)element.Element("description") ?? ""
            };

            items.Add(item);
        }

        return items;
    }
}
