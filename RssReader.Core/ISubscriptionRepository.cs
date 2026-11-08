// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

namespace RssReader.Core;

public interface ISubscriptionRepository
{
	Task<List<FeedSubscription>> GetAllAsync();

	Task AddAsync(FeedSubscription subscription);

	Task<bool> RemoveAsync(string url);

	Task<bool> UpdateTitleAsync(string url, string title);

	Task<bool> UpdateLastRetrievedAsync(
		string url,
		DateTimeOffset lastRetrieved);
}
