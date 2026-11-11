// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

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
	public async Task<bool> IsSubscribedAsync(string url)
	{
		List<FeedSubscription> subscriptions =
				await _repository.GetAllAsync();

		return subscriptions.Any(subscription =>
SubscriptionUrl.Matches(subscription.Url, url));
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

		if (!SubscriptionUrl.IsValid(subscription.Url))
		{
			throw new ArgumentException(
							"The subscription URL must be an absolute HTTP or HTTPS URL.",
							nameof(subscription));
		}

		List<FeedSubscription> existingSubscriptions =
				await _repository.GetAllAsync();

		bool alreadyExists = existingSubscriptions.Any(existing =>
		SubscriptionUrl.Matches(existing.Url, subscription.Url));

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
		SubscriptionUrl.Matches(existing.Url, url));

		if (subscription is null)
		{
			return false;
		}

		return await _repository.RemoveAsync(subscription.Url);
	}
	public async Task<bool> RenameAsync(string url, string title)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			throw new ArgumentException(
					"The subscription title cannot be empty.",
					nameof(title));
		}

		List<FeedSubscription> existingSubscriptions =
				await _repository.GetAllAsync();

		FeedSubscription? subscription = existingSubscriptions
				.FirstOrDefault(existing =>
						SubscriptionUrl.Matches(existing.Url, url));

		if (subscription is null)
		{
			return false;
		}

		return await _repository.UpdateTitleAsync(
				subscription.Url,
				title.Trim());
	}
	public async Task<bool> UpdateLastRetrievedAsync(string url, DateTimeOffset lastRetrieved)
	{
		return await _repository.UpdateLastRetrievedAsync(
				url,
				lastRetrieved);
	}
}
