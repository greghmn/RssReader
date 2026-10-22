
using System.Transactions;

namespace RssReader.Core;

public class SubscriptionService
{
    private readonly ISubscriptionRepository _repository;

    public SubscriptionService(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<FeedSubscription>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<bool> AddAsync(FeedSubscription subscription)
    {
        /*
         Current order: 
        1. Validate URL syntax and scheme
        2. Retrieve existing subscriptions 
        3. Check for duplicates 
        4. Save subscription if not present already
         */
        if (!Uri.TryCreate(
        subscription.Url,
        UriKind.Absolute,
        out Uri? feedUri)
    || (feedUri.Scheme != Uri.UriSchemeHttp
        && feedUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "The subscription URL must be an absolute HTTP or HTTPS URL.",
                nameof(subscription));
        }
        List<FeedSubscription> existingSubscriptions =
            await _repository.GetAllAsync();

        bool alreadyExists = existingSubscriptions.Any(existing =>
            string.Equals(
                existing.Url.TrimEnd('/'),
                subscription.Url.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            return false;
        }

        await _repository.AddAsync(subscription);
        return true;
    }

    public async Task<bool> RemoveAsync(string url)
    {
        List<FeedSubscription> existingSubscriptions =
            await _repository.GetAllAsync();

        FeedSubscription? subscription = existingSubscriptions
            .FirstOrDefault(existing =>
                string.Equals(
                    existing.Url.TrimEnd('/'),
                    url.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase));

        if (subscription is null)
        {
            return false;
        }

        return await _repository.RemoveAsync(subscription.Url);
    }
}
