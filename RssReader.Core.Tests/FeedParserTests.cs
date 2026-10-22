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

    [Fact]
    public void Parse_RssItemWithoutDescription_UsesEmptyString()
    {
        XDocument document = XDocument.Parse("""
        <rss version="2.0">
          <channel>
            <item>
              <title>Article without description</title>
              <link>https://example.com/article</link>
            </item>
          </channel>
        </rss>
        """);

        List<FeedItem> items = FeedParser.Parse(document);

        Assert.Single(items);
        Assert.Equal("", items[0].Description);
    }

    [Fact]
    public void Parse_AtomEntryWithSelfAndAlternateLinks_PrefersAlternate()
    {
        XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>Example Feed</title>
          <entry>
            <title>Example Article</title>
            <link rel="self" href="https://example.com/api/article" />
            <link rel="alternate" href="https://example.com/article" />
            <summary>Example summary</summary>
          </entry>
        </feed>
        """);

        List<FeedItem> items = FeedParser.Parse(document);

        Assert.Single(items);
        Assert.Equal(
            "https://example.com/article",
            items[0].Link);
    }

    [Fact]
    public void Parse_EmptyRssFeed_ReturnsEmptyList()
    {
        XDocument document = XDocument.Parse("""
        <rss version="2.0">
          <channel>
            <title>Empty Feed</title>
          </channel>
        </rss>
        """);

        List<FeedItem> items = FeedParser.Parse(document);

        Assert.Empty(items);
    }

    [Fact]
    public void Parse_RssItemWithPubDate_ParsesPublishedAt()
    {
        // Arrange
        XDocument document = XDocument.Parse("""
        <rss version="2.0">
            <channel>
                <title>Example Feed</title>
                <item>
                    <title>Example Article</title>
                    <link>https://example.com/article</link>
                    <description>An example.</description>
                    <pubDate>Wed, 21 Oct 2015 07:28:00 GMT</pubDate>
                </item>
            </channel>
        </rss>
        """);

        // Act
        List<FeedItem> items = FeedParser.Parse(document);

        // Assert
        Assert.Single(items);
        Assert.Equal(
            DateTimeOffset.Parse("Wed, 21 Oct 2015 07:28:00 GMT"),
            items[0].PublishedAt);
    }

    [Fact]
    public void Parse_AtomEntryWithPublishedAndUpdated_PrefersPublished()
    {
        // Arrange
        XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <link href="https://example.com/article" />
                <published>2020-01-15T10:00:00Z</published>
                <updated>2020-02-20T12:00:00Z</updated>
                <summary>An example.</summary>
            </entry>
        </feed>
        """);

        // Act
        List<FeedItem> items = FeedParser.Parse(document);

        // Assert
        Assert.Single(items);
        Assert.Equal(
            DateTimeOffset.Parse("2020-01-15T10:00:00Z"),
            items[0].PublishedAt);
    }

    [Fact]
    public void Parse_AtomEntryWithoutPublished_UsesUpdated()
    {
        // Arrange
        XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Updated Article</title>
                <link href="https://example.com/article" />
                <updated>2024-06-10T14:30:00Z</updated>
                <summary>An updated article.</summary>
            </entry>
        </feed>
        """);

        // Act
        List<FeedItem> items = FeedParser.Parse(document);

        // Assert
        Assert.Single(items);
        Assert.Equal(
            DateTimeOffset.Parse("2024-06-10T14:30:00Z"),
            items[0].PublishedAt);
    }

    //Test when no data is provided
    [Fact]
    public void Parse_AtomEntryWithoutDates_ReturnsNullPublishedAt()
    {
        // Arrange
        XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Undated Article</title>
                <link href="https://example.com/article" />
                <summary>An article without dates.</summary>
            </entry>
        </feed>
        """);

        // Act
        List<FeedItem> items = FeedParser.Parse(document);

        // Assert
        Assert.Single(items);
        Assert.Null(items[0].PublishedAt);
    }

    //Test when published is invalid but updated is valid
    [Fact]
    public void Parse_AtomEntryWithInvalidPublished_UsesUpdated()
    {
        // Arrange
        XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Article With Invalid Date</title>
                <link href="https://example.com/article" />
                <published>not-a-date</published>
                <updated>2024-06-10T14:30:00Z</updated>
                <summary>An article with a malformed date.</summary>
            </entry>
        </feed>
        """);

        // Act
        List<FeedItem> items = FeedParser.Parse(document);

        // Assert
        Assert.Single(items);
        Assert.Equal(
            DateTimeOffset.Parse("2024-06-10T14:30:00Z"),
            items[0].PublishedAt);
    }


}
