
using System.Xml.Linq;

namespace RssReader.Core;

public static class OpmlImporter
{
	public static List<FeedSubscription> Import(string xml)
	{
		XDocument document = XDocument.Parse(xml);

		if (document.Root?.Name != "opml")
		{
			throw new FormatException(
					"The document is not a valid OPML document.");
		}

		XElement? body = document.Root?.Element("body");

		if (body is null)
		{
			throw new FormatException(
					"The OPML document has no body element.");
		}

		var subscriptions = new List<FeedSubscription>();

		foreach (XElement outline in body.Descendants("outline"))
		{
			string? url = (string?)outline.Attribute("xmlUrl");

			if (string.IsNullOrWhiteSpace(url))
			{
				continue;
			}

			string? title = (string?)outline.Attribute("title");

			if (string.IsNullOrWhiteSpace(title))
			{
				title = (string?)outline.Attribute("text");
			}

			title ??= "";

			subscriptions.Add(new FeedSubscription
			{
				Url = url,
				Title = title
			});
		}

		return subscriptions;
	}
}
