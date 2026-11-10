// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
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
				"An example article.", items[0].Summary);
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
				"An Atom example.", items[0].Summary);
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
		Assert.Equal("", items[0].Summary);
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

	//Harden the feed parsing
	[Fact]
	public void Parse_RssItemWithDublinCoreDate_ParsesPublishedAt()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <rss version="2.0"
             xmlns:dc="http://purl.org/dc/elements/1.1/">
            <channel>
                <title>Example Feed</title>
                <item>
                    <title>Example Article</title>
                    <link>https://example.com/article</link>
                    <dc:date>2024-06-10T14:30:00Z</dc:date>
                </item>
            </channel>
        </rss>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				DateTimeOffset.Parse("2024-06-10T14:30:00Z"),
				items[0].PublishedAt);
	}

	[Fact]
	public void Parse_RssItemWithEstPubDate_ParsesPublishedAt()
	{
		XDocument document = XDocument.Parse("""
    <rss version="2.0">
        <channel>
            <title>ESPN News</title>
            <item>
                <title>Example Article</title>
                <pubDate>Mon, 9 Nov 2026 18:02:07 EST</pubDate>
            </item>
        </channel>
    </rss>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);

		Assert.Equal(
						new DateTimeOffset(
										2026, 11, 9, 18, 2, 7,
										TimeSpan.FromHours(-5)),
						items[0].PublishedAt);
	}

	[Fact]
	public void Parse_RssItemWithEdtPubDate_ParsesPublishedAt()
	{
		XDocument document = XDocument.Parse("""
    <rss version="2.0">
        <channel>
            <title>ESPN News</title>
            <item>
                <title>Example Article</title>
                <pubDate>Mon, 6 Jul 2026 18:02:07 EDT</pubDate>
            </item>
        </channel>
    </rss>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);

		Assert.Equal(
						new DateTimeOffset(
										2026, 7, 6, 18, 2, 7,
										TimeSpan.FromHours(-4)),
						items[0].PublishedAt);
	}

	//For RSS items that are missing titles 

	[Fact]
	public void Parse_RssItemWithoutTitle_ReturnsEmptyTitle()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <rss version="2.0">
            <channel>
                <title>Example Feed</title>
                <item>
                    <link>https://example.com/article</link>
                    <description>An article without a title.</description>
                </item>
            </channel>
        </rss>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal("", items[0].Title);
		Assert.Equal(
				"https://example.com/article",
				items[0].Link);
	}

	//Get Atom parser to select alternate links 

	[Fact]
	public void Parse_AtomEntryWithMultipleLinks_PrefersAlternateLink()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <link rel="self"
                      href="https://example.com/api/article/123" />
                <link rel="alternate"
                      href="https://example.com/article/123" />
                <summary>An example article.</summary>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"https://example.com/article/123",
				items[0].Link);
	}

	//Skip Atom links that have empty href values 

	[Fact]
	public void Parse_AtomEntryWithEmptyAlternateLink_PrefersValidAlternateLink()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <link rel="alternate" href="" />
                <link rel="alternate"
                      href="https://example.com/article" />
                <summary>An example article.</summary>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"https://example.com/article",
				items[0].Link);
	}

	//For handling relative Atom links 

	[Fact]
	public void Parse_AtomEntryWithRelativeLink_ResolvesAgainstFeedUrl()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <link rel="alternate" href="/articles/123" />
                <summary>An example article.</summary>
            </entry>
        </feed>
        """);

		Uri feedUrl = new(
				"https://example.com/feeds/tech.xml");

		// Act
		List<FeedItem> items = FeedParser.Parse(document, feedUrl);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"https://example.com/articles/123",
				items[0].Link);
	}

	//For handling HTTP redirects to ensure it works with the final URL 

	[Fact]
	public void Parse_AtomEntryWithRelativeLink_UsesFinalFeedUrl()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <link rel="alternate" href="/articles/123" />
            </entry>
        </feed>
        """);

		Uri finalFeedUrl = new(
				"https://cdn.example.com/feeds/tech.xml");

		// Act
		List<FeedItem> items =
				FeedParser.Parse(document, finalFeedUrl);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"https://cdn.example.com/articles/123",
				items[0].Link);
	}

	//For handling the Atom xml:base attribute properly 

	[Fact]
	public void Parse_AtomEntryWithXmlBase_ResolvesRelativeLink()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom"
              xml:base="https://example.com/content/">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <link rel="alternate" href="article-123" />
            </entry>
        </feed>
        """);

		Uri feedUrl = new(
				"https://example.com/feeds/tech.xml");

		// Act
		List<FeedItem> items = FeedParser.Parse(document, feedUrl);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"https://example.com/content/article-123",
				items[0].Link);
	}
	//For nested xml:base 

	[Fact]
	public void Parse_AtomEntryWithNestedXmlBase_ResolvesRelativeLink()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom"
              xml:base="https://example.com/content/">
            <title>Example Feed</title>
            <entry xml:base="articles/">
                <title>Example Article</title>
                <link rel="alternate"
                      xml:base="2026/"
                      href="article-123" />
            </entry>
        </feed>
        """);

		Uri feedUrl = new(
				"https://example.com/feeds/tech.xml");

		// Act
		List<FeedItem> items = FeedParser.Parse(document, feedUrl);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"https://example.com/content/articles/2026/article-123",
				items[0].Link);
	}


	[Fact]
	public void Parse_AtomEntryWithContentButNoSummary_ExtractsContent()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Article With Content</title>
                <link href="https://example.com/article" />
                <content>Article body text.</content>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"Article body text.",
				items[0].Content);
	}



	[Fact]
	public void Parse_AtomEntryWithXhtmlContent_ExtractsText()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>HTML Article</title>
                <link href="https://example.com/article" />
                <content type="xhtml">
                    <div xmlns="http://www.w3.org/1999/xhtml">
                        <p>First paragraph.</p>
                        <p>Second paragraph.</p>
                        <p>A third paragraph with
                            <strong>emphasis</strong>.
                        </p>
                    </div>
                </content>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"First paragraph.\nSecond paragraph.\nA third paragraph with emphasis.",
				items[0].Content);
	}

	[Fact]

	public void Parse_AtomEntryWithHtmlContent_ExtractsReadableText()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>HTML Article</title>
                <link href="https://example.com/article" />
                <content type="html">&lt;p&gt;Hello &lt;strong&gt;world&lt;/strong&gt;.&lt;/p&gt;&lt;p&gt;Second paragraph.&lt;/p&gt;</content>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"Hello world.\nSecond paragraph.",
				items[0].Content);
	}

	[Fact]
	public void Parse_AtomEntryWithMalformedHtmlContent_ExtractsReadableText()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Imperfect HTML</title>
                <link href="https://example.com/article" />
                <content type="html">&lt;p&gt;First paragraph.&lt;p&gt;Second paragraph.</content>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"First paragraph.\nSecond paragraph.",
				items[0].Content);
	}

	[Fact]
	public void Parse_AtomEntryWithHtmlContent_IgnoresScriptAndStyle()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Article</title>
                <link href="https://example.com/article" />
                <content type="html">&lt;p&gt;Readable text.&lt;/p&gt;&lt;script&gt;alert('hello');&lt;/script&gt;&lt;style&gt;p { color: red; }&lt;/style&gt;&lt;p&gt;More readable text.&lt;/p&gt;</content>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"Readable text.\nMore readable text.",
				items[0].Content);
	}

	[Fact]
	public void Parse_AtomEntryWithHtmlContent_DecodesEntitiesAndNormalizesWhitespace()
	{
		// Arrange
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Article</title>
                <link href="https://example.com/article" />
                <content type="html">&lt;p&gt;Fish &amp;amp; chips&amp;nbsp; are   delicious.&lt;/p&gt;&lt;p&gt;Tea &amp; coffee.&lt;/p&gt;</content>
            </entry>
        </feed>
        """);

		// Act
		List<FeedItem> items = FeedParser.Parse(document);

		// Assert
		Assert.Single(items);
		Assert.Equal(
				"Fish & chips are delicious.\nTea & coffee.",
				items[0].Content);
	}


	[Fact]
	public void Parse_RssItemWithGuid_ExtractsId()
	{
		XDocument document = XDocument.Parse("""
        <rss version="2.0">
            <channel>
                <title>Example Feed</title>
                <item>
                    <title>Example Article</title>
                    <link>https://example.com/article</link>
                    <guid>article-123</guid>
                </item>
            </channel>
        </rss>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Equal("article-123", items[0].Id);
	}

	[Fact]
	public void Parse_AtomEntryWithId_ExtractsId()
	{
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <id>urn:example:article:123</id>
                <title>Example Article</title>
                <link href="https://example.com/article" />
            </entry>
        </feed>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Equal("urn:example:article:123", items[0].Id);
	}

	//dealing with missing ID 

	[Fact]
	public void Parse_RssItemWithoutGuid_ReturnsEmptyId()
	{
		XDocument document = XDocument.Parse("""
        <rss version="2.0">
            <channel>
                <item>
                    <title>Article without GUID</title>
                    <link>https://example.com/article</link>
                </item>
            </channel>
        </rss>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Equal("", items[0].Id);
	}

	[Fact]
	public void Parse_AtomEntryWithoutId_ReturnsEmptyId()
	{
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <entry>
                <title>Article without ID</title>
                <link href="https://example.com/article" />
            </entry>
        </feed>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Equal("", items[0].Id);
	}

	//for testing how HTML in RSS description is handled
	[Fact]
	public void Parse_RssDescriptionWithHtml_ExtractsReadableText()
	{
		XDocument document = XDocument.Parse(
				"""
        <rss version="2.0">
            <channel>
                <title>Test Feed</title>
                <item>
                    <title>Test Article</title>
                    <description><![CDATA[
                        <p>Hello <strong>world</strong>!</p>
                        <p>This is a second paragraph.</p>
                        <script>alert('test');</script>
                        <style>body { display: none; }</style>
                    ]]></description>
                    <guid>article-1</guid>
                </item>
            </channel>
        </rss>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		FeedItem item = Assert.Single(items);

		Assert.Equal(
				"Hello world!\nThis is a second paragraph.",
				item.Summary);
	}

	//same but for HTML in Atom 
	//also this is test #67 officially

	[Fact]
	public void Parse_AtomHtmlSummary_ExtractsReadableText()
	{
		XDocument document = XDocument.Parse(
				"""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Test Feed</title>
            <entry>
                <title>Test Article</title>
                <id>article-1</id>
                <summary type="html"><![CDATA[
                    <p>Hello <strong>world</strong>!</p>
                    <p>This is a second paragraph.</p>
                    <script>alert('test');</script>
                    <style>body { display: none; }</style>
                ]]></summary>
            </entry>
        </feed>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		FeedItem item = Assert.Single(items);

		Assert.Equal(
				"Hello world!\nThis is a second paragraph.",
				item.Summary);
	}

	//Atom again, but for XHTML

	[Fact]
	public void Parse_AtomXhtmlSummary_ExtractsReadableText()
	{
		XDocument document = XDocument.Parse(
				"""
        <feed xmlns="http://www.w3.org/2005/Atom"
              xmlns:xhtml="http://www.w3.org/1999/xhtml">
            <title>Test Feed</title>
            <entry>
                <title>Test Article</title>
                <id>article-1</id>
                <summary type="xhtml">
                    <xhtml:div>
                        <xhtml:p>Hello <xhtml:strong>world</xhtml:strong>!</xhtml:p>
                        <xhtml:p>This is a second paragraph.</xhtml:p>
                    </xhtml:div>
                </summary>
            </entry>
        </feed>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		FeedItem item = Assert.Single(items);

		Assert.Equal(
				"Hello world!\nThis is a second paragraph.",
				item.Summary);
	}

	//Atom with plaintext 
	//Test #69, nice! 

	[Fact]
	public void Parse_AtomPlainTextSummary_PreservesText()
	{
		XDocument document = XDocument.Parse(
				"""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Test Feed</title>
            <entry>
                <title>Test Article</title>
                <id>article-1</id>
                <summary type="text">
                    This is plain text with &lt;angle brackets&gt;
                    and &amp; an entity.
                </summary>
            </entry>
        </feed>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		FeedItem item = Assert.Single(items);

		Assert.Contains("<angle brackets>", item.Summary);
		Assert.Contains("& an entity.", item.Summary);
	}

	// Atom content should be extracted independently of summary.
	[Fact]
	public void Parse_AtomEntryWithoutSummary_ExtractsContent()
	{
		XDocument document = XDocument.Parse(
				"""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Test Feed</title>
            <entry>
                <title>Test Article</title>
                <id>article-1</id>
                <content type="html"><![CDATA[
                    <p>Content without a summary.</p>
                    <p>Another paragraph with <strong>emphasis</strong>.</p>
                ]]></content>
            </entry>
        </feed>
        """);

		List<FeedItem> items = FeedParser.Parse(document);

		FeedItem item = Assert.Single(items);

		Assert.Equal(
				"Content without a summary.\nAnother paragraph with emphasis.",
				item.Content);
	}

	[Fact]
	public void Parse_RssItemWithContentEncoded_ExtractsContent()
	{
		XDocument document = XDocument.Parse("""
        <rss version="2.0"
             xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Example Feed</title>
                <item>
                    <title>Example Article</title>
                    <description>A short summary.</description>
                    <content:encoded><![CDATA[
                        <p>The full article body.</p>
                        <p>A second paragraph.</p>
                    ]]></content:encoded>
                </item>
            </channel>
        </rss>
        """);

		FeedItem item = Assert.Single(FeedParser.Parse(document));

		Assert.Equal("A short summary.", item.Summary);
		Assert.Equal(
				"The full article body.\nA second paragraph.",
				item.Content);
	}

	[Fact]
	public void Parse_AtomEntryWithoutContent_UsesEmptyContent()
	{
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <summary>A short summary.</summary>
            </entry>
        </feed>
        """);

		FeedItem item = Assert.Single(FeedParser.Parse(document));

		Assert.Equal("A short summary.", item.Summary);
		Assert.Equal("", item.Content);
	}
	[Fact]
	public void Parse_AtomEntryWithSummaryAndContent_ExtractsBoth()
	{
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <entry>
                <title>Example Article</title>
                <summary>A short summary.</summary>
                <content>
                    The complete article body.
                </content>
            </entry>
        </feed>
        """);

		FeedItem item = Assert.Single(FeedParser.Parse(document));

		Assert.Equal("A short summary.", item.Summary);
		Assert.Equal("The complete article body.", item.Content.Trim());
	}


	[Fact]
	public void ParseFeedTitle_Rss_ReturnsChannelTitle()
	{
		XDocument document = XDocument.Parse("""
        <rss version="2.0">
            <channel>
                <title>Example News</title>
                <description>A sample RSS feed.</description>
                <item>
                    <title>First Article</title>
                </item>
            </channel>
        </rss>
        """);

		string? title = FeedParser.ParseFeedTitle(document);

		Assert.Equal("Example News", title);
	}


	[Fact]
	public void ParseFeedTitle_Atom_ReturnsFeedTitle()
	{
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Science Weekly</title>
            <id>https://example.com/feed</id>
            <updated>2026-11-06T12:00:00Z</updated>
            <entry>
                <title>New Discovery</title>
                <id>https://example.com/article-1</id>
                <updated>2026-11-06T12:00:00Z</updated>
            </entry>
        </feed>
        """);

		string? title = FeedParser.ParseFeedTitle(document);

		Assert.Equal("Science Weekly", title);
	}


	[Fact]
	public void ParseFeedTitle_RssMissingTitle_ReturnsNull()
	{
		XDocument document = XDocument.Parse("""
        <rss version="2.0">
            <channel>
                <description>A feed without a title.</description>
            </channel>
        </rss>
        """);

		string? title = FeedParser.ParseFeedTitle(document);

		Assert.Null(title);
	}

	[Fact]
	public void ParseFeedTitle_AtomWhitespaceTitle_ReturnsNull()
	{
		XDocument document = XDocument.Parse("""
        <feed xmlns="http://www.w3.org/2005/Atom">
            <title>   </title>
            <id>https://example.com/feed</id>
            <updated>2026-11-06T12:00:00Z</updated>
        </feed>
        """);

		string? title = FeedParser.ParseFeedTitle(document);

		Assert.Null(title);
	}


	[Fact]
	public void Parse_RssItemWithInvalidPubDate_PreservesRawDate()
	{
		const string rawDate = "A very strange Tuesday";

		XDocument document = XDocument.Parse($"""
    <rss version="2.0">
        <channel>
            <title>Example Feed</title>
            <item>
                <title>Example Article</title>
                <pubDate>{rawDate}</pubDate>
            </item>
        </channel>
    </rss>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Null(items[0].PublishedAt);
		Assert.Equal(rawDate, items[0].RawPublishedDate);
	}

	[Fact]
	public void Parse_RssItemWithValidPubDate_PreservesRawDate()
	{
		const string rawDate = "Mon, 9 Nov 2026 18:02:07 EST";

		XDocument document = XDocument.Parse($"""
    <rss version="2.0">
        <channel>
            <title>Example Feed</title>
            <item>
                <title>Example Article</title>
                <pubDate>{rawDate}</pubDate>
            </item>
        </channel>
    </rss>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);

		Assert.Equal(rawDate, items[0].RawPublishedDate);

		Assert.Equal(
						new DateTimeOffset(
										2026, 11, 9, 18, 2, 7,
										TimeSpan.FromHours(-5)),
						items[0].PublishedAt);
	}
	[Fact]
	public void Parse_RssItemWithoutDate_ReturnsNullRawPublishedDate()
	{
		XDocument document = XDocument.Parse("""
    <rss version="2.0">
        <channel>
            <title>Example Feed</title>
            <item>
                <title>Undated Article</title>
                <description>No publication date supplied.</description>
            </item>
        </channel>
    </rss>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Null(items[0].PublishedAt);
		Assert.Null(items[0].RawPublishedDate);
	}

	[Fact]
	public void Parse_RssInvalidPubDateWithValidDublinCoreDate_PreservesBoth()
	{
		const string rawDate = "An unrecognized date";

		XDocument document = XDocument.Parse($"""
    <rss version="2.0"
         xmlns:dc="http://purl.org/dc/elements/1.1/">
        <channel>
            <title>Example Feed</title>
            <item>
                <title>Example Article</title>
                <pubDate>{rawDate}</pubDate>
                <dc:date>2026-11-09T23:02:07Z</dc:date>
            </item>
        </channel>
    </rss>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Equal(rawDate, items[0].RawPublishedDate);

		Assert.Equal(
						new DateTimeOffset(
										2026, 11, 9, 23, 2, 7,
										TimeSpan.Zero),
						items[0].PublishedAt);
	}

	[Fact]
	public void Parse_AtomEntryWithPublished_PreservesRawDate()
	{
		const string rawDate = "2026-11-09T18:02:07-05:00";

		XDocument document = XDocument.Parse($"""
    <feed xmlns="http://www.w3.org/2005/Atom">
        <title>Example Feed</title>
        <entry>
            <title>Example Article</title>
            <id>https://example.com/article</id>
            <published>{rawDate}</published>
        </entry>
    </feed>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);

		Assert.Equal(rawDate, items[0].RawPublishedDate);

		Assert.Equal(
						new DateTimeOffset(
										2026, 11, 9, 18, 2, 7,
										TimeSpan.FromHours(-5)),
						items[0].PublishedAt);
	}

	[Fact]
	public void Parse_AtomEntryWithoutPublished_PreservesRawUpdatedDate()
	{
		const string rawDate = "2026-11-09T23:02:07Z";

		XDocument document = XDocument.Parse($"""
    <feed xmlns="http://www.w3.org/2005/Atom">
        <title>Example Feed</title>
        <entry>
            <title>Example Article</title>
            <id>https://example.com/article</id>
            <updated>{rawDate}</updated>
        </entry>
    </feed>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);

		Assert.Equal(rawDate, items[0].RawPublishedDate);

		Assert.Equal(
						new DateTimeOffset(
										2026, 11, 9, 23, 2, 7,
										TimeSpan.Zero),
						items[0].PublishedAt);
	}

	[Fact]
	public void Parse_AtomEntryWithoutDates_ReturnsNullRawPublishedDate()
	{
		XDocument document = XDocument.Parse("""
    <feed xmlns="http://www.w3.org/2005/Atom">
        <title>Example Feed</title>
        <entry>
            <title>Undated Article</title>
            <id>https://example.com/article</id>
        </entry>
    </feed>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Null(items[0].PublishedAt);
		Assert.Null(items[0].RawPublishedDate);
	}

	[Fact]
	public void Parse_AtomInvalidPublishedWithValidUpdated_PreservesRawDate()
	{
		const string rawDate = "An unrecognized date";

		XDocument document = XDocument.Parse($"""
    <feed xmlns="http://www.w3.org/2005/Atom">
        <title>Example Feed</title>
        <entry>
            <title>Example Article</title>
            <id>https://example.com/article</id>
            <published>{rawDate}</published>
            <updated>2026-11-09T23:02:07Z</updated>
        </entry>
    </feed>
    """);

		List<FeedItem> items = FeedParser.Parse(document);

		Assert.Single(items);
		Assert.Equal(rawDate, items[0].RawPublishedDate);

		Assert.Equal(
						new DateTimeOffset(
										2026, 11, 9, 23, 2, 7,
										TimeSpan.Zero),
						items[0].PublishedAt);
	}
}
