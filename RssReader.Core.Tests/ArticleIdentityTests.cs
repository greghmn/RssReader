
using Xunit;

namespace RssReader.Core.Tests;

public class ArticleIdentityTests
{
    [Fact]
    public void GetId_WhenFeedIdExists_ReturnsFeedId()
    {
        FeedItem item = new()
        {
            Id = "article-123",
            Link = "https://example.com/article"
        };

        Assert.Equal("article-123", ArticleIdentity.GetId(item));
    }

    [Fact]
    public void GetId_WhenFeedIdIsMissing_ReturnsLink()
    {
        FeedItem item = new()
        {
            Link = "https://example.com/article"
        };

        Assert.Equal(
            "https://example.com/article",
            ArticleIdentity.GetId(item));
    }

    [Fact]
    public void GetId_WhenFeedIdIsWhitespace_ReturnsLink()
    {
        FeedItem item = new()
        {
            Id = "   ",
            Link = "https://example.com/article"
        };

        Assert.Equal(
            "https://example.com/article",
            ArticleIdentity.GetId(item));
    }

    [Fact]
    public void GetId_WhenBothIdAndLinkAreMissing_ReturnsNull()
    {
        FeedItem item = new();

        Assert.Null(ArticleIdentity.GetId(item));
    }
}
