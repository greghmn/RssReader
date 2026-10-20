
using System.Xml.Linq;

namespace RssReader.Core;

public static class FeedParser
{
    private static readonly XNamespace Atom =
        "http://www.w3.org/2005/Atom";

    public static List<FeedItem> Parse(XDocument document)
    {
        XElement? root = document.Root;

        if (root is null)
        {
            throw new InvalidDataException(
                "The document has no root element.");
        }

        if (root.Name.LocalName == "rss")
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
                    Description =
                        GetElementValue(element, "description")
                })
                .ToList();
        }

        if (root.Name == Atom + "feed")
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

        throw new InvalidDataException(
            $"Unsupported feed format: {root.Name}");
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
