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

	[Fact]
	public void Export_WithNoSubscriptions_ProducesEmptyOpmlBody()
	{
		var service = new OpmlService();

		string xml = service.Export(Array.Empty<FeedSubscription>());

		XDocument document = XDocument.Parse(xml);

		Assert.Equal("opml", document.Root?.Name.LocalName);
		Assert.Equal(
				"2.0",
				(string?)document.Root?.Attribute("version"));

		XElement? body = document.Root?.Element("body");

		Assert.NotNull(body);
		Assert.Empty(body.Elements("outline"));
	}

	[Fact]
	public void Export_WithSpecialCharacters_PreservesSubscriptionTitle()
	{
		var service = new OpmlService();

		const string title = "Science & Tech <News> \"Daily\"";

		var subscriptions = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/feed.xml",
						Title = title
				}
		};

		string xml = service.Export(subscriptions);

		XDocument document = XDocument.Parse(xml);

		XElement outline = Assert.Single(
				document.Root!
						.Element("body")!
						.Elements("outline"));

		Assert.Equal(
				title,
				(string?)outline.Attribute("text"));

		Assert.Equal(
				title,
				(string?)outline.Attribute("title"));

		Assert.Contains("&amp;", xml);
		Assert.Contains("&lt;News&gt;", xml);
		Assert.Contains("&quot;Daily&quot;", xml);
	}

	[Fact]
	public void Import_WithOneSubscription_ReturnsFeedSubscription()
	{
		var service = new OpmlService();

		const string xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <opml version="2.0">
          <head>
            <title>My Subscriptions</title>
          </head>
          <body>
            <outline
              type="rss"
              text="Example Feed"
              title="Example Feed"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </opml>
        """;

		List<FeedSubscription> subscriptions = service.Import(xml);

		FeedSubscription subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscription.Url);

		Assert.Equal(
				"Example Feed",
				subscription.Title);
	}

	[Fact]
	public void Import_WithNestedFolders_ReturnsOnlyFeedSubscriptions()
	{
		var service = new OpmlService();

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="Technology">
              <outline text="Programming">
                <outline text="C# Weekly"
                         xmlUrl="https://example.com/csharp.xml" />
              </outline>
              <outline text="Security News"
                       xmlUrl="https://example.com/security.xml" />
            </outline>
            <outline text="Science Feed"
                     xmlUrl="https://example.com/science.xml" />
          </body>
        </opml>
        """;

		List<FeedSubscription> subscriptions = service.Import(xml);

		Assert.Equal(3, subscriptions.Count);

		Assert.Equal(
				new[]
				{
						"https://example.com/csharp.xml",
						"https://example.com/security.xml",
						"https://example.com/science.xml"
				},
				subscriptions.Select(subscription => subscription.Url));
	}

	[Theory]
	[InlineData("Preferred Title", "Fallback Text", "Preferred Title")]
	[InlineData(null, "Text Only Feed", "Text Only Feed")]
	[InlineData("", "Empty Title Fallback", "Empty Title Fallback")]
	[InlineData("   ", "Whitespace Title Fallback", "Whitespace Title Fallback")]
	public void Import_WhenTitleIsMissingOrBlank_UsesText(
		string? title,
		string text,
		string expectedTitle)
	{
		var service = new OpmlService();

		var outline = new XElement("outline",
				new XAttribute("text", text),
				new XAttribute("xmlUrl", "https://example.com/feed.xml"));

		if (title is not null)
		{
			outline.SetAttributeValue("title", title);
		}

		var document = new XDocument(
				new XElement("opml",
						new XAttribute("version", "2.0"),
						new XElement("body", outline)));

		FeedSubscription subscription =
				Assert.Single(service.Import(document.ToString()));

		Assert.Equal(expectedTitle, subscription.Title);
	}
}