using System.Xml.Linq;
using System.Globalization;

namespace RssReader.Core;

public static class FeedParser
{
    private static readonly XNamespace Atom =
        "http://www.w3.org/2005/Atom";
    private static readonly XNamespace DublinCore =
    "http://purl.org/dc/elements/1.1/";

    public static List<FeedItem> Parse(XDocument document, Uri? baseUri = null)
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
                        GetElementValue(element, "description"),
                    PublishedAt = ParseDate(GetElementValue(element, "pubDate")) ?? ParseDate((string?)element.Element(DublinCore + "date") ?? "")
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
                    Link = GetAtomLink(element, baseUri),
                    Description =
                        (string?)element.Element(Atom + "summary")
                        ?? (string?)element.Element(Atom + "content")
                        ?? "",
                    PublishedAt = ParseDate(
                        (string?)element.Element(Atom + "published")
                        ?? "")
                    ?? ParseDate(
                        (string?)element.Element(Atom + "updated")
                          ?? "")
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
    private static DateTimeOffset? ParseDate(string value)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTimeOffset date))
        {
            return date;
        }

        return null;
    }

    //Updated to continue if the first alternate link can't be used and accept nullable Uri parameter and resolve relative links 

    private static string GetAtomLink(
        XElement entry,
        Uri? baseUri)
    {
        XElement? link = entry
            .Elements(Atom + "link")
            .FirstOrDefault(element =>
                ((string?)element.Attribute("rel") is null
                    or "alternate")
                && !string.IsNullOrWhiteSpace(
                    (string?)element.Attribute("href")));

        string href =
            (string?)link?.Attribute("href") ?? "";

        if (string.IsNullOrWhiteSpace(href))
        {
            return "";
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.AbsoluteUri;
        }

        if (baseUri is not null
            && Uri.TryCreate(baseUri, href, out Uri? resolvedUri))
        {
            return resolvedUri.AbsoluteUri;
        }

        return href;
    }


}
