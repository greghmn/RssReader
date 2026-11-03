
using RssReader.Core;

namespace RssReader.Core.Tests;

public class FakeSubscriptionRepository : ISubscriptionRepository
{
	private readonly List<FeedSubscription> _subscriptions = [];

	public Task<List<FeedSubscription>> GetAllAsync()
	{
		return Task.FromResult(_subscriptions.ToList());
	}

	public Task AddAsync(FeedSubscription subscription)
	{
		_subscriptions.Add(subscription);
		return Task.CompletedTask;
	}

	public Task<bool> RemoveAsync(string url)
	{
		FeedSubscription? subscription = _subscriptions
				.FirstOrDefault(existing => existing.Url == url);

		if (subscription is null)
		{
			return Task.FromResult(false);
		}

		_subscriptions.Remove(subscription);
		return Task.FromResult(true);
	}

	public Task<bool> UpdateTitleAsync(string url, string title)
	{
		FeedSubscription? subscription = _subscriptions
				.FirstOrDefault(existing => existing.Url == url);

		if (subscription is null)
		{
			return Task.FromResult(false);
		}

		subscription.Title = title;
		return Task.FromResult(true);
	}

}
