using RssReader.Core;

namespace RssReader.Core.Tests;

public class FeedServiceTests
{
    [Fact]
    public async Task GetItemsAsync_UnsubscribedUrl_ThrowsException()
    {
        // Arrange
        var subscriptionService = new SubscriptionService();
        var feedReader = new FeedReader();

        var feedService = new FeedService(
            subscriptionService,
            feedReader);

        // Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => feedService.GetItemsAsync(
                "https://example.com/feed.xml"));
    }

    [Fact]
    public async Task GetItemsAsync_SubscribedUrl_ReturnsFeedItems()
    {
        // Arrange
        var subscriptionService = new SubscriptionService();

        subscriptionService.Add(new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        });

        var expectedItems = new List<FeedItem>
    {
        new()
        {
            Title = "First Article",
            Link = "https://example.com/article-1",
            Description = "The first article."
        },
        new()
        {
            Title = "Second Article",
            Link = "https://example.com/article-2",
            Description = "The second article."
        }
    };

        var fakeReader = new FakeFeedReader
        {
            ItemsToReturn = expectedItems
        };

        var feedService = new FeedService(
            subscriptionService,
            fakeReader);

        // Act
        List<FeedItem> actualItems =
            await feedService.GetItemsAsync(
                "https://example.com/feed.xml");

        // Assert
        Assert.Equal(expectedItems, actualItems);
        Assert.Equal(
            "https://example.com/feed.xml",
            fakeReader.LastRequestedUrl);
    }

}
