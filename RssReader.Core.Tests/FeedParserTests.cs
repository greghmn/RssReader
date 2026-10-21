
using System.Xml.Linq;
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class FeedParserTests
{
    [Fact]
    public void Parse_RssFeed_ReturnsItems()
    {
        XDocument document = XDocument.Parse("""
            <rss version="2.0">
              <channel>
                <title>Example Feed</title>
                <item>
                  <title>First article</title>
                  <link>https://example.com/first</link>
                  <description>An example article.</description>
                </item>
              </channel>
            </rss>
            """);

        List<FeedItem> items = FeedParser.Parse(document);

        Assert.Single(items);
        Assert.Equal("First article", items[0].Title);
        Assert.Equal(
            "https://example.com/first", items[0].Link);
        Assert.Equal(
            "An example article.", items[0].Description);
    }

    [Fact]
    public void Parse_AtomFeed_ReturnsItems()
    {
        XDocument document = XDocument.Parse("""
            <feed xmlns="http://www.w3.org/2005/Atom">
              <title>Example Feed</title>
              <entry>
                <title>Atom article</title>
                <link href="https://example.com/atom-article" />
                <summary>An Atom example.</summary>
              </entry>
            </feed>
            """);

        List<FeedItem> items = FeedParser.Parse(document);

        Assert.Single(items);
        Assert.Equal("Atom article", items[0].Title);
        Assert.Equal(
            "https://example.com/atom-article", items[0].Link);
        Assert.Equal(
            "An Atom example.", items[0].Description);
    }

    [Fact]
    public void Parse_UnsupportedDocument_Throws()
    {
        XDocument document = XDocument.Parse(
            "<html><body>Not a feed</body></html>");

        Assert.Throws<InvalidDataException>(
            () => FeedParser.Parse(document));
    }
}
