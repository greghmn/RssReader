
using Xunit;

namespace RssReader.Core.Tests;

public class OpmlImporterTests
{
	[Fact]
	public void Import_WithOneSubscription_ReturnsSubscription()
	{
		const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <opml version="2.0">
              <head>
                <title>My Subscriptions</title>
              </head>
              <body>
                <outline
                  type="rss"
                  text="Example News"
                  title="Example News"
                  xmlUrl="https://example.com/feed.xml" />
              </body>
            </opml>
            """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscription.Url);

		Assert.Equal(
				"Example News",
				subscription.Title);
	}
	[Fact]
	public void Import_WithNestedOutlines_FindsSubscriptions()
	{
		const string xml = """
        <opml version="2.0">
          <head />
          <body>
            <outline text="Technology">
              <outline
                text="Tech News"
                xmlUrl="https://example.com/tech.xml" />
              <outline text="Programming">
                <outline
                  text="Programming News"
                  xmlUrl="https://example.com/programming.xml" />
              </outline>
            </outline>
            <outline
              text="General News"
              xmlUrl="https://example.com/news.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		Assert.Equal(3, subscriptions.Count);

		Assert.Equal(
				"https://example.com/tech.xml",
				subscriptions[0].Url);

		Assert.Equal(
				"https://example.com/programming.xml",
				subscriptions[1].Url);

		Assert.Equal(
				"https://example.com/news.xml",
				subscriptions[2].Url);
	}
	[Fact]
	public void Import_WhenTitleMissing_UsesTextAttribute()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              text="Example Technology Feed"
              xmlUrl="https://example.com/tech.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/tech.xml",
				subscription.Url);

		Assert.Equal(
				"Example Technology Feed",
				subscription.Title);
	}
	[Fact]
	public void Import_WhenTitleEmpty_UsesTextAttribute()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              title=""
              text="Example Technology Feed"
              xmlUrl="https://example.com/tech.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"Example Technology Feed",
				subscription.Title);
	}
	[Fact]
	public void Import_WhenTitleWhitespace_UsesTextAttribute()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              title="   "
              text="Example Technology Feed"
              xmlUrl="https://example.com/tech.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"Example Technology Feed",
				subscription.Title);
	}
	[Fact]
	public void Import_WhenXmlUrlMissing_SkipsOutline()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="Technology" />
            <outline
              text="Example News"
              xmlUrl="https://example.com/feed.xml" />
            <outline
              text="Another Folder">
              <outline
                text="Nested News"
                xmlUrl="https://example.com/nested.xml" />
            </outline>
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		Assert.Equal(2, subscriptions.Count);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscriptions[0].Url);

		Assert.Equal(
				"https://example.com/nested.xml",
				subscriptions[1].Url);
	}
	[Fact]
	public void Import_WhenXmlUrlEmpty_SkipsOutline()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              text="Broken Feed"
              xmlUrl="" />
            <outline
              text="Valid Feed"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscription.Url);

		Assert.Equal(
				"Valid Feed",
				subscription.Title);
	}
	[Fact]
	public void Import_WhenXmlUrlWhitespace_SkipsOutline()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              text="Invalid Feed"
              xmlUrl="   " />
            <outline
              text="Valid Feed"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscription.Url);

		Assert.Equal(
				"Valid Feed",
				subscription.Title);
	}
	[Fact]
	public void Import_WithMalformedXml_ThrowsXmlException()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              text="Example News"
              xmlUrl="https://example.com/feed.xml">
          </body>
        </opml>
        """;

		Assert.Throws<System.Xml.XmlException>(
				() => OpmlImporter.Import(xml));
	}
	[Fact]
	public void Import_WithIncorrectRoot_ThrowsFormatException()
	{
		const string xml = """
        <notOpml>
          <body>
            <outline
              text="Example News"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </notOpml>
        """;

		Assert.Throws<FormatException>(
				() => OpmlImporter.Import(xml));
	}
	[Fact]
	public void Import_WithMissingBody_ThrowsFormatException()
	{
		const string xml = """
        <opml version="2.0">
          <head>
            <title>My Subscriptions</title>
          </head>
        </opml>
        """;

		Assert.Throws<FormatException>(
				() => OpmlImporter.Import(xml));
	}
	[Theory]
	[InlineData("1.0")]
	[InlineData("1.1")]
	[InlineData("2.0")]
	public void Import_WithSupportedOpmlVersion_ReturnsSubscription(string version)
	{
		string xml = $"""
        <opml version="{version}">
          <body>
            <outline
              text="Example News"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscription.Url);

		Assert.Equal(
				"Example News",
				subscription.Title);
	}
	[Fact]
	public void Import_WithoutVersion_ReturnsSubscription()
	{
		const string xml = """
        <opml>
          <body>
            <outline
              text="Example News"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"https://example.com/feed.xml",
				subscription.Url);

		Assert.Equal(
				"Example News",
				subscription.Title);
	}

	[Fact]
	public void Import_WithInvalidFeedUrl_PreservesCandidate()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              text="Invalid Feed"
              xmlUrl="not-a-valid-url" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		var subscription = Assert.Single(subscriptions);

		Assert.Equal(
				"not-a-valid-url",
				subscription.Url);

		Assert.Equal(
				"Invalid Feed",
				subscription.Title);
	}

	[Fact]
	public void Import_WithDuplicateUrls_PreservesBothCandidates()
	{
		const string xml = """
        <opml version="2.0">
          <body>
            <outline
              text="Original Title"
              xmlUrl="https://example.com/feed.xml" />
            <outline
              text="Alternative Title"
              xmlUrl="https://example.com/feed.xml" />
          </body>
        </opml>
        """;

		var subscriptions = OpmlImporter.Import(xml);

		Assert.Equal(2, subscriptions.Count);

		Assert.Equal(
				"Original Title",
				subscriptions[0].Title);

		Assert.Equal(
				"Alternative Title",
				subscriptions[1].Title);

		Assert.Equal(
				subscriptions[0].Url,
				subscriptions[1].Url);
	}

}
