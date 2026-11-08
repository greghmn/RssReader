// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

namespace RssReader.Core;

public interface IFeedReader
{
	Task<List<FeedItem>> ReadAsync(
			string feedUrl,
			CancellationToken cancellationToken = default);

	Task<string?> ReadTitleAsync(
				string feedUrl,
				CancellationToken cancellationToken = default);
}
