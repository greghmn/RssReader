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
}
