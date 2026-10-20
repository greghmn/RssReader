
using System.Xml.Linq;

namespace RssReader.Core;

public class FeedReader
{
    private static readonly HttpClient HttpClient = new();

    private static readonly XNamespace Atom =
        "http://www.w3.org/2005/Atom";

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

        XElement? root = document.Root;

        if (root is null)
        {
            throw new InvalidDataException(
                "The document has no root element.");
        }

        if (root.Name.LocalName == "rss")
        {
            return ReadRss(root);
        }

        if (root.Name == Atom + "feed")
        {
            return ReadAtom(root);
        }

        throw new InvalidDataException(
            $"Unsupported feed format: {root.Name}");
    }

    private static List<FeedItem> ReadRss(XElement root)
    {
        XElement? channel = root
            .Elements()
            .FirstOrDefault(element =>
                element.Name.LocalName == "channel");

        if (channel is null)
        {
            throw new InvalidDataException(
                "The RSS document has no channel element.");
        }

        return channel
            .Elements()
            .Where(element => element.Name.LocalName == "item")
            .Select(element => new FeedItem
            {
                Title = GetElementValue(element, "title"),
                Link = GetElementValue(element, "link"),
                Description = GetElementValue(
                    element, "description")
            })
            .ToList();
    }

    private static List<FeedItem> ReadAtom(XElement root)
    {
        return root
            .Elements(Atom + "entry")
            .Select(element => new FeedItem
            {
                Title = (string?)element.Element(Atom + "title")
                    ?? "",
                Link = GetAtomLink(element),
                Description =
                    (string?)element.Element(Atom + "summary")
                    ?? (string?)element.Element(Atom + "content")
                    ?? ""
            })
            .ToList();
    }


    private static string GetElementValue(
        XElement parent, string name)
    {
        XElement? element = parent
            .Elements()
            .FirstOrDefault(element =>
                element.Name.LocalName == name);

        return (string?)element ?? "";
    }


    private static string GetAtomLink(XElement entry)
    {
        XElement? link = entry
            .Elements(Atom + "link")
            .FirstOrDefault(element =>
                (string?)element.Attribute("rel") is null
                or "alternate");

        return (string?)link?.Attribute("href") ?? "";
    }
}
