// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
namespace RssReader.Core;

public interface IArticleRepository
{
	Task<List<FeedItem>> GetByFeedAsync(string feedUrl);

	Task SaveAsync(
			string feedUrl,
			IEnumerable<FeedItem> articles);
}