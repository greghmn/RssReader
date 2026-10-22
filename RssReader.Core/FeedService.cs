
namespace RssReader.Core;

public class FeedService
{
    private readonly SubscriptionService _subscriptionService;
    private readonly IFeedReader _feedReader;

    public FeedService(
        SubscriptionService subscriptionService,
        IFeedReader feedReader)
    {
        _subscriptionService = subscriptionService;
        _feedReader = feedReader;
    }

    public async Task<List<FeedItem>> GetItemsAsync(string feedUrl)
    {
        List<FeedSubscription> subscriptions =
            await _subscriptionService.GetAllAsync();

        bool isSubscribed = subscriptions.Any(subscription =>
            string.Equals(
                subscription.Url.TrimEnd('/'),
                feedUrl.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));

        if (!isSubscribed)
        {
            throw new InvalidOperationException(
                "Cannot read a feed that is not subscribed to.");
        }

        return await _feedReader.ReadAsync(feedUrl);
    }
}
