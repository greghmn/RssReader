
using RssReader.Core;

namespace RssReader.Core.Tests;

public class SubscriptionServiceTests
{
    [Fact]
    public async Task AddAsync_ValidSubscription_AddsIt()
    {
        // Arrange
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var subscription = new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        };

        // Act
        bool result = await service.AddAsync(subscription);
        List<FeedSubscription> subscriptions =
            await service.GetAllAsync();

        // Assert
        Assert.True(result);
        Assert.Single(subscriptions);
        Assert.Equal(subscription.Url, subscriptions[0].Url);
    }

    [Fact]
    public async Task AddAsync_DuplicateUrl_ReturnsFalse()
    {
        // Arrange
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        await service.AddAsync(new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        });

        // Act
        bool result = await service.AddAsync(new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Duplicate Feed"
        });

        List<FeedSubscription> subscriptions =
            await service.GetAllAsync();

        // Assert
        Assert.False(result);
        Assert.Single(subscriptions);
    }

    [Fact]
    public async Task RemoveAsync_ExistingSubscription_RemovesIt()
    {
        // Arrange
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        await service.AddAsync(new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        });

        // Act
        bool result = await service.RemoveAsync(
            "https://example.com/feed.xml");

        List<FeedSubscription> subscriptions =
            await service.GetAllAsync();

        // Assert
        Assert.True(result);
        Assert.Empty(subscriptions);
    }

    [Fact]
    public async Task RemoveAsync_UnknownUrl_ReturnsFalse()
    {
        // Arrange
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        // Act
        bool result = await service.RemoveAsync(
            "https://example.com/missing.xml");

        // Assert
        Assert.False(result);
        Assert.Empty(await service.GetAllAsync());
    }
}
