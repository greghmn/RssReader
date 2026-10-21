
namespace RssReader.Core;

public class SubscriptionService
{
    private readonly List<FeedSubscription> _subscriptions = [];

    public IReadOnlyList<FeedSubscription> GetAll()
    {
        return _subscriptions.AsReadOnly();
    }

    public bool Add(FeedSubscription subscription)
    {
        bool alreadyExists = _subscriptions.Any(existing =>
            string.Equals(
                existing.Url.TrimEnd('/'),
                subscription.Url.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            return false;
        }

        _subscriptions.Add(subscription);
        return true;
    }

    public bool Remove(string url)
    {
        FeedSubscription? subscription = _subscriptions
            .FirstOrDefault(existing =>
                string.Equals(
                    existing.Url.TrimEnd('/'),
                    url.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase));

        if (subscription is null)
        {
            return false;
        }

        _subscriptions.Remove(subscription);
        return true;
    }
}
