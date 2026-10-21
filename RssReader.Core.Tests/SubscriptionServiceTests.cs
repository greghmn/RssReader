
using RssReader.Core;

namespace RssReader.Core.Tests;

public class SubscriptionServiceTests
{
    [Fact]
    public void Add_ValidSubscription_AddsIt()
    {
        // Arrange
        var service = new SubscriptionService();
        var subscription = new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        };

        // Act
        bool result = service.Add(subscription);

        // Assert
        Assert.True(result);
        Assert.Single(service.GetAll());
        Assert.Equal(subscription, service.GetAll()[0]);
    }

    [Fact]
    public void Add_DuplicateUrl_ReturnsFalse()
    {
        // Arrange
        var service = new SubscriptionService();
        var subscription = new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        };

        service.Add(subscription);

        // Act
        bool result = service.Add(new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Duplicate Feed"
        });

        // Assert
        Assert.False(result);
        Assert.Single(service.GetAll());
    }

    [Fact]
    public void Remove_ExistingSubscription_RemovesIt()
    {
        // Arrange
        var service = new SubscriptionService();
        var subscription = new FeedSubscription
        {
            Url = "https://example.com/feed.xml",
            Title = "Example Feed"
        };

        service.Add(subscription);

        // Act
        bool result = service.Remove(subscription.Url);

        // Assert
        Assert.True(result);
        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void Remove_UnknownUrl_ReturnsFalse()
    {
        // Arrange
        var service = new SubscriptionService();

        // Act
        bool result = service.Remove("https://example.com/missing.xml");

        // Assert
        Assert.False(result);
        Assert.Empty(service.GetAll());
    }
}
