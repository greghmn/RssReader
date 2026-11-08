// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
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
		return ImportWithIssues(xml).Subscriptions;
	}

	public OpmlImportResult ImportWithIssues(string xml)
	{
		XDocument document = XDocument.Parse(xml);
		XElement? body = document.Root?.Element("body");

		if (document.Root?.Name != "opml" || body is null)
		{
			throw new FormatException("Invalid OPML document.");
		}

		var result = new OpmlImportResult();

		var seenUrls = new HashSet<string>(
		StringComparer.OrdinalIgnoreCase);

		foreach (XElement outline in body.Descendants("outline"))
		{
			string? url = (string?)outline.Attribute("xmlUrl");
			string? type = (string?)outline.Attribute("type");

			bool isRss = string.Equals(
					type,
					"rss",
					StringComparison.OrdinalIgnoreCase);

			// Ignore folders, but report recognizable feeds with missing URLs.
			if (url is null && !isRss)
			{
				continue;
			}

			string? title = (string?)outline.Attribute("title");
			string? text = (string?)outline.Attribute("text");

			string displayName = !string.IsNullOrWhiteSpace(title)
					? title
					: text ?? "";

			if (url is null)
			{
				result.Issues.Add(new OpmlImportIssue
				{
					Url = null,
					Title = displayName,
					Reason = "Missing feed URL."
				});

				continue;
			}

			bool isValidUrl =
					Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
					&& (uri.Scheme == Uri.UriSchemeHttp
							|| uri.Scheme == Uri.UriSchemeHttps);

			if (!isValidUrl)
			{
				result.Issues.Add(new OpmlImportIssue
				{
					Url = url,
					Title = displayName,
					Reason = "Invalid feed URL."
				});

				continue;
			}

			string normalizedUrl = url.TrimEnd('/');

			if (!seenUrls.Add(normalizedUrl))
			{
				result.Issues.Add(new OpmlImportIssue
				{
					Url = url,
					Title = displayName,
					Reason = "Duplicate feed URL."
				});

				continue;
			}

			result.Subscriptions.Add(new FeedSubscription
			{
				Url = url,
				Title = displayName
			});
		}

		return result;
	}
}