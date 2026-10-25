using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using HtmlAgilityPack;

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
                    ?? ExtractAtomContent(element),
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

    //Also for dealing with xml:base to adjust to the actual URL needed
    private static string GetAtomLink(XElement entry, Uri? baseUri)
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

        if (Uri.TryCreate(
            href, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.AbsoluteUri;
        }

        if (link is not null)
        {
            Uri? effectiveBase = ResolveBaseUri(link, baseUri);

            if (effectiveBase is not null
                && Uri.TryCreate(
                    effectiveBase,
                    href,
                    out Uri? resolvedUri))
            {
                return resolvedUri.AbsoluteUri;
            }
        }

        return href;
    }


    private static string ExtractReadableText(XElement element)
    {
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";

        HashSet<string> blockElements =
        [
            "p", "div", "section", "article",
        "blockquote", "li", "h1", "h2",
        "h3", "h4", "h5", "h6", "br"
        ];

        StringBuilder text = new();

        void AppendNode(XNode node)
        {
            if (node is XText textNode)
            {
                string value = textNode.Value;

                // Normalize whitespace within each text node while preserving
                // spaces around inline elements.
                string normalized = string.Join(
                    " ",
                    value.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries));

                if (normalized.Length > 0)
                {
                    if (char.IsWhiteSpace(value[0]))
                    {
                        text.Append(' ');
                    }

                    text.Append(normalized);

                    if (char.IsWhiteSpace(value[^1]))
                    {
                        text.Append(' ');
                    }
                }
                else if (value.Length > 0)
                {
                    text.Append(' ');
                }
            }

            else if (node is XElement child)
            {
                bool isBlock =
                    child.Name.Namespace == xhtml
                    && blockElements.Contains(child.Name.LocalName);

                if (isBlock && text.Length > 0
                    && text[^1] != '\n')
                {
                    text.Append('\n');
                }

                foreach (XNode childNode in child.Nodes())
                {
                    AppendNode(childNode);
                }

                if (isBlock && text.Length > 0
                    && text[^1] != '\n')
                {
                    text.Append('\n');
                }
            }
        }

        foreach (XNode node in element.Nodes())
        {
            AppendNode(node);
        }

        string[] lines = text.ToString().Split('\n');

        return string.Join(
            "\n",
            lines.Select(line =>
                string.Join(
                    " ",
                    line.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))));
    }


    //Helper for the previous function 
    private static Uri? ResolveBaseUri(
        XElement element,
        Uri? baseUri)
    {
        Uri? currentBase = baseUri;

        foreach (XElement ancestor in element
            .AncestorsAndSelf()
            .Reverse())
        {
            string? xmlBase =
                (string?)ancestor.Attribute(
                    XNamespace.Xml + "base");

            if (string.IsNullOrWhiteSpace(xmlBase))
            {
                continue;
            }

            if (currentBase is not null
                && Uri.TryCreate(
                    currentBase,
                    xmlBase,
                    out Uri? resolvedBase))
            {
                currentBase = resolvedBase;
            }
            else if (Uri.TryCreate(
                xmlBase,
                UriKind.Absolute,
                out Uri? absoluteBase))
            {
                currentBase = absoluteBase;
            }
        }

        return currentBase;
    }

    private static string ExtractAtomContent(XElement entry)
    {
        XElement? content = entry.Element(Atom + "content");

        if (content is null)
        {
            return "";
        }

        string? contentType = (string?)content.Attribute("type");

        if (contentType == "xhtml")
        {
            return ExtractReadableText(content)
                .Trim();
        }


        if (contentType == "html")
        {
            HtmlDocument htmlDocument = new();

            htmlDocument.LoadHtml(content.Value);

            return ExtractReadableHtmlText(
                htmlDocument.DocumentNode).Trim();
        }


        return content.Value;
    }

    private static string ExtractReadableHtmlText(HtmlNode node)
    {
        HashSet<string> blockElements =
        [
            "p", "div", "section", "article",
        "blockquote", "li", "h1", "h2",
        "h3", "h4", "h5", "h6", "br"
        ];

        StringBuilder text = new();

        void AppendNode(HtmlNode current)
        {
            if (current.NodeType == HtmlNodeType.Text)
            {
                text.Append(
                    HtmlEntity.DeEntitize(current.InnerText));
                return;
            }

            bool isBlock =
                current.NodeType == HtmlNodeType.Element
                && blockElements.Contains(current.Name);

            if (isBlock && text.Length > 0
                && text[^1] != '\n')
            {
                text.Append('\n');
            }

            foreach (HtmlNode child in current.ChildNodes)
            {
                AppendNode(child);
            }

            if (isBlock && text.Length > 0
                && text[^1] != '\n')
            {
                text.Append('\n');
            }
        }

        AppendNode(node);

        string[] lines = text.ToString().Split('\n');

        return string.Join(
            "\n",
            lines.Select(line =>
                string.Join(
                    " ",
                    line.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))));
    }



}
