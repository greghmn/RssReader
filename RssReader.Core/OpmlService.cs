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
				.Select(outline =>
				{
					string? title = (string?)outline.Attribute("title");
					string? text = (string?)outline.Attribute("text");

					return new FeedSubscription
					{
						Url = (string?)outline.Attribute("xmlUrl") ?? "",
						Title = !string.IsNullOrWhiteSpace(title)
									? title
									: text ?? ""
					};
				})
				.ToList();
	}

	public OpmlImportResult ImportWithIssues(string xml)
	{
		List<FeedSubscription> subscriptions = Import(xml);

		var result = new OpmlImportResult();

		foreach (FeedSubscription subscription in subscriptions)
		{
			bool isValidUrl =
					Uri.TryCreate(
							subscription.Url,
							UriKind.Absolute,
							out Uri? uri)
					&& (uri.Scheme == Uri.UriSchemeHttp
							|| uri.Scheme == Uri.UriSchemeHttps);

			if (!isValidUrl)
			{
				result.Issues.Add(new OpmlImportIssue
				{
					Url = subscription.Url,
					Title = subscription.Title,
					Reason = "Invalid feed URL."
				});

				continue;
			}

			result.Subscriptions.Add(subscription);
		}

		return result;
	}
}