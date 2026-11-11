
using System.Xml.Linq;
using Xunit;

namespace RssReader.Core.Tests;

public class OpmlExporterTests
{
	[Fact]
	public void Export_WithOneSubscription_ProducesValidOpml()
	{
		var subscriptions = new[]
		{
						new FeedSubscription
						{
								Url = "https://example.com/feed.xml",
								Title = "Example News"
						}
				};

		string xml = OpmlExporter.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		Assert.Equal("opml", document.Root?.Name.LocalName);
		Assert.Equal("2.0", (string?)document.Root?.Attribute("version"));

		XElement? outline = document
						.Root?
						.Element("body")?
						.Element("outline");

		Assert.NotNull(outline);
		Assert.Equal(
						"https://example.com/feed.xml",
						(string?)outline.Attribute("xmlUrl"));
		Assert.Equal(
						"Example News",
						(string?)outline.Attribute("text"));
		Assert.Equal(
						"Example News",
						(string?)outline.Attribute("title"));
		Assert.Equal(
						"rss",
						(string?)outline.Attribute("type"));
	}
	[Fact]
	public void Export_WithMultipleSubscriptions_PreservesOrder()
	{
		var subscriptions = new[]
		{
				new FeedSubscription
				{
						Url = "https://example.com/first.xml",
						Title = "First Feed"
				},
				new FeedSubscription
				{
						Url = "https://example.com/second.xml",
						Title = "Second Feed"
				},
				new FeedSubscription
				{
						Url = "https://example.com/third.xml",
						Title = "Third Feed"
				}
		};

		string xml = OpmlExporter.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		var outlines = document.Root!
						.Element("body")!
						.Elements("outline")
						.ToList();

		Assert.Equal(3, outlines.Count);

		Assert.Equal(
				"https://example.com/first.xml",
				(string?)outlines[0].Attribute("xmlUrl"));

		Assert.Equal(
				"https://example.com/second.xml",
				(string?)outlines[1].Attribute("xmlUrl"));

		Assert.Equal(
				"https://example.com/third.xml",
				(string?)outlines[2].Attribute("xmlUrl"));
	}
	[Fact]
	public void Export_WithNoSubscriptions_ProducesEmptyBody()
	{
		var subscriptions = Array.Empty<FeedSubscription>();

		string xml = OpmlExporter.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		Assert.Equal("opml", document.Root?.Name.LocalName);

		XElement? body = document.Root?.Element("body");

		Assert.NotNull(body);
		Assert.Empty(body.Elements());
	}
	[Fact]
	public void Export_WithSpecialCharacters_PreservesTitle()
	{
		const string title =
						"Science & Technology <Daily> \"Special Edition\"";

		var subscriptions = new[]
		{
				new FeedSubscription
				{
						Url = "https://example.com/feed?topic=science&lang=en",
						Title = title
				}
		};

		string xml = OpmlExporter.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		XElement? outline = document.Root?
						.Element("body")?
						.Element("outline");

		Assert.NotNull(outline);

		Assert.Equal(
						title,
						(string?)outline.Attribute("text"));

		Assert.Equal(
						title,
						(string?)outline.Attribute("title"));

		Assert.Equal(
						"https://example.com/feed?topic=science&lang=en",
						(string?)outline.Attribute("xmlUrl"));
	}
	[Fact]
	public void Export_IncludesXmlDeclaration()
	{
		string xml = OpmlExporter.Export(
						Array.Empty<FeedSubscription>());

		Assert.StartsWith(
						"<?xml version=\"1.0\" encoding=\"utf-8\"?>",
						xml);
	}
	[Fact]
	public void Export_WithEmptyTitle_PreservesEmptyTitle()
	{
		var subscriptions = new[]
		{
				new FeedSubscription
				{
						Url = "https://example.com/feed.xml",
						Title = ""
				}
		};

		string xml = OpmlExporter.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		XElement? outline = document.Root?
						.Element("body")?
						.Element("outline");

		Assert.NotNull(outline);

		Assert.Equal(
						"",
						(string?)outline.Attribute("text"));

		Assert.Equal(
						"",
						(string?)outline.Attribute("title"));

		Assert.Equal(
						"https://example.com/feed.xml",
						(string?)outline.Attribute("xmlUrl"));
	}


}
