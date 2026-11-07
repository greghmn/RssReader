using RssReader.Core;

namespace RssReader.Core.Tests;

public class FakeFeedReader : IFeedReader
{
	public List<FeedItem> ItemsToReturn { get; set; } = [];

	public string? LastRequestedUrl { get; private set; }

	public int ReadCount { get; private set; }

	public string? TitleToReturn { get; set; }

	public Exception? TitleException { get; set; }

	public int TitleReadCount { get; private set; }

	//implement the interface 

	public Task<List<FeedItem>> ReadAsync(
			string feedUrl,
			CancellationToken cancellationToken = default)
	{
		ReadCount++;
		LastRequestedUrl = feedUrl;

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