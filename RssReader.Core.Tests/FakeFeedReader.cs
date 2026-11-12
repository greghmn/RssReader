// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using RssReader.Core;

namespace RssReader.Core.Tests;

public class FakeFeedReader : IFeedReader
{
	public List<FeedItem> ItemsToReturn { get; set; } = [];

	public string? LastRequestedUrl { get; private set; }

	public int ReadCount { get; private set; }

	public string? TitleToReturn { get; set; }

	public Exception? TitleException { get; set; }

	public Exception? ReadException { get; set; }

	public int TitleReadCount { get; private set; }

	//implement the interface 

	public Task<List<FeedItem>> ReadAsync(
					string feedUrl,
					CancellationToken cancellationToken = default)
	{
		ReadCount++;
		LastRequestedUrl = feedUrl;

		if (ReadException is not null)
		{
			throw ReadException;
		}

		return Task.FromResult(ItemsToReturn);
	}


	public Task<string?> ReadTitleAsync(
					string feedUrl,
					CancellationToken cancellationToken = default)
	{
		TitleReadCount++;

		if (TitleException is not null)
		{
			throw TitleException;
		}

		return Task.FromResult(TitleToReturn);
	}

}
