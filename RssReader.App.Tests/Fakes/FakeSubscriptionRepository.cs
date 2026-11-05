using RssReader.Core;

namespace RssReader.App.Tests.Fakes;

public class FakeSubscriptionRepository : ISubscriptionRepository
{
	public List<FeedSubscription> Subscriptions { get; } = new();

	public bool RemoveResult { get; set; } = true;

	public int UpdateLastRetrievedCount { get; private set; }

	public Exception? GetAllException { get; set; }

	public Exception? RemoveException { get; set; }

	public TaskCompletionSource<bool>? RemoveStarted { get; set; }

	public TaskCompletionSource<bool>? AllowRemove { get; set; }

	public TaskCompletionSource<bool>? RemoveCompletion { get; set; }

	public bool UpdateTitleResult { get; set; } = true;

	public Exception? UpdateTitleException { get; set; }

	public TaskCompletionSource<bool>? UpdateTitleStarted { get; set; }

	public TaskCompletionSource<bool>? AllowUpdateTitle { get; set; }

	public TaskCompletionSource<bool>? UpdateTitleCompletion { get; set; }

	public Task<List<FeedSubscription>> GetAllAsync()
	{
		if (GetAllException is not null)
		{
			throw GetAllException;
		}

		return Task.FromResult(Subscriptions.ToList());
	}

	public Task AddAsync(FeedSubscription subscription)
	{
		Subscriptions.Add(subscription);

		return Task.CompletedTask;
	}

	public async Task<bool> RemoveAsync(string url)
	{
		if (RemoveException is not null)
		{
			throw RemoveException;
		}

		RemoveStarted?.TrySetResult(true);

		if (AllowRemove is not null)
		{
			await AllowRemove.Task;
		}

		if (RemoveCompletion is not null)
		{
			return await RemoveCompletion.Task;
		}

		if (!RemoveResult)
		{
			return false;
		}

		FeedSubscription? subscription = Subscriptions.FirstOrDefault(
				s => string.Equals(
						s.Url.TrimEnd('/'),
						url.TrimEnd('/'),
						StringComparison.OrdinalIgnoreCase));

		if (subscription is null)
		{
			return false;
		}

		Subscriptions.Remove(subscription);

		return true;
	}

	public async Task<bool> UpdateTitleAsync(string url, string title)
	{
		if (UpdateTitleException is not null)
		{
			throw UpdateTitleException;
		}

		UpdateTitleStarted?.TrySetResult(true);

		if (AllowUpdateTitle is not null)
		{
			await AllowUpdateTitle.Task;
		}

		if (UpdateTitleCompletion is not null)
		{
			return await UpdateTitleCompletion.Task;
		}

		if (!UpdateTitleResult)
		{
			return false;
		}

		FeedSubscription? subscription = Subscriptions.FirstOrDefault(
				s => string.Equals(
						s.Url.TrimEnd('/'),
						url.TrimEnd('/'),
						StringComparison.OrdinalIgnoreCase));

		if (subscription is null)
		{
			return false;
		}

		subscription.Title = title;

		return true;
	}

	public Task<bool> UpdateLastRetrievedAsync(string url, DateTimeOffset lastRetrieved)
	{
		UpdateLastRetrievedCount++;

		FeedSubscription? subscription = Subscriptions.FirstOrDefault(
				s => string.Equals(
						s.Url.TrimEnd('/'),
						url.TrimEnd('/'),
						StringComparison.OrdinalIgnoreCase));

		if (subscription is null)
		{
			return Task.FromResult(false);
		}

		subscription.LastRetrieved = lastRetrieved;

		return Task.FromResult(true);
	}
}