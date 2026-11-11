using Xunit;
namespace RssReader.Core.Tests;

public class SubscriptionUrlTests
{
	[Theory]
	[InlineData("https://example.com/feed", "https://example.com/feed")]
	[InlineData("https://example.com/feed", "https://example.com/feed/")]
	[InlineData("https://example.com/feed", "HTTPS://EXAMPLE.COM/FEED")]
	public void Matches_WithEquivalentUrls_ReturnsTrue(string first, string second)
	{
		Assert.True(SubscriptionUrl.Matches(first, second));
	}

	[Theory]
	[InlineData("https://example.com/feed.xml", true)]
	[InlineData("http://example.com/feed.xml", true)]
	[InlineData("HTTPS://example.com/feed.xml", true)]
	[InlineData("not-a-valid-url", false)]
	[InlineData("/relative/feed.xml", false)]
	[InlineData("ftp://example.com/feed.xml", false)]
	[InlineData("file:///tmp/feed.xml", false)]
	[InlineData("", false)]
	public void IsValid_WithVariousUrls_ReturnsExpectedResult(string url,bool expected)
	{
		Assert.Equal(expected, SubscriptionUrl.IsValid(url));
	}

}
