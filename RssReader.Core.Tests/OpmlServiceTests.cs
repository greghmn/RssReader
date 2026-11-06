using System.Xml.Linq;
using RssReader.Core;

namespace RssReader.Core.Tests;

public class OpmlServiceTests
{
	[Fact]
	public void Export_WithOneSubscription_ProducesValidOpml()
	{
		var service = new OpmlService();

		var subscriptions = new List<FeedSubscription>
				{
						new()
						{
								Url = "https://example.com/feed.xml",
								Title = "Example Feed"
						}
				};

		string xml = service.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		Assert.Equal("opml", document.Root?.Name.LocalName);
		Assert.Equal("2.0", (string?)document.Root?.Attribute("version"));

		XElement? head = document.Root?.Element("head");

		Assert.Equal(
				"RSS Reader Subscriptions",
				(string?)head?.Element("title"));

		XElement? body = document.Root?.Element("body");

		Assert.NotNull(body);

		XElement outline = Assert.Single(body.Elements("outline"));

		Assert.Equal(
				"rss",
				(string?)outline.Attribute("type"));

		Assert.Equal(
				"Example Feed",
				(string?)outline.Attribute("text"));

		Assert.Equal(
				"Example Feed",
				(string?)outline.Attribute("title"));

		Assert.Equal(
				"https://example.com/feed.xml",
				(string?)outline.Attribute("xmlUrl"));
	}

	[Fact]
	public void Export_WithMultipleSubscriptions_PreservesOrder()
	{
		var service = new OpmlService();

		var subscriptions = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/first.xml",
						Title = "First Feed"
				},
				new()
				{
						Url = "https://example.com/second.xml",
						Title = "Second Feed"
				},
				new()
				{
						Url = "https://example.com/third.xml",
						Title = "Third Feed"
				}
		};

		string xml = service.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		List<XElement> outlines = document.Root!
				.Element("body")!
				.Elements("outline")
				.ToList();

		Assert.Equal(3, outlines.Count);

		Assert.Equal(
				new[]
				{
						"https://example.com/first.xml",
						"https://example.com/second.xml",
						"https://example.com/third.xml"
				},
				outlines.Select(outline =>
						(string?)outline.Attribute("xmlUrl")));
	}
}