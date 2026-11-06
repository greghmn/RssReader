using System.Xml.Linq;

namespace RssReader.Core;

public class OpmlService
{
	public string Export(IEnumerable<FeedSubscription> subscriptions)
	{
		var document = new XDocument(
			new XDeclaration("1.0", "utf-8", null),
			new XElement("opml",
				new XAttribute("version", "2.0"),
				new XElement("head",
					new XElement("title", "RSS Reader Subscriptions")
				),
				new XElement("body",
					subscriptions.Select(subscription =>
						new XElement("outline",
							new XAttribute("type", "rss"),
							new XAttribute("text", subscription.Title),
							new XAttribute("title", subscription.Title),
							new XAttribute("xmlUrl", subscription.Url)
						)
					)
				)
			)
		);

		return document.ToString();
	}
	public List<FeedSubscription> Import(string xml)
	{
		XDocument document = XDocument.Parse(xml);

		XElement? body = document.Root?.Element("body");

		if (document.Root?.Name != "opml" || body is null)
		{
			throw new FormatException("Invalid OPML document.");
		}

		return body.Descendants("outline")
				.Where(outline => outline.Attribute("xmlUrl") is not null)
				.Select(outline => new FeedSubscription
				{
					Url = (string?)outline.Attribute("xmlUrl") ?? "",
					Title = (string?)outline.Attribute("title") ?? ""
				})
				.ToList();
	}
}