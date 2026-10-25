
using HtmlAgilityPack;
using System.Text;
using System.Xml.Linq;

namespace RssReader.Core;

internal static class FeedContentExtractor
{
    internal static string ExtractReadableText(XElement element)
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
    internal static string ExtractReadableHtmlText(HtmlNode node)
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
            if (current.NodeType == HtmlNodeType.Element
                && (current.Name.Equals(
                        "script",
                        StringComparison.OrdinalIgnoreCase)
                    || current.Name.Equals(
                        "style",
                        StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

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
