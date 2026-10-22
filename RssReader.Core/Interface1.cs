
namespace RssReader.Core;

public interface ISubscriptionRepository
{
    Task<List<FeedSubscription>> GetAllAsync();

    Task AddAsync(FeedSubscription subscription);

    Task<bool> RemoveAsync(string url);
}
