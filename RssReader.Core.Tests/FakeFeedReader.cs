using RssReader.Core;

namespace RssReader.Core.Tests;

public class FakeFeedReader : IFeedReader
{
	public List<FeedItem> ItemsToReturn { get; set; } = [];

	public string? LastRequestedUrl { get; private set; }

	public int ReadCount { get; private set; }

	//implement the interface 

	public Task<List<FeedItem>> ReadAsync(
			string feedUrl,
			CancellationToken cancellationToken = default)
	{
		ReadCount++;
		LastRequestedUrl = feedUrl;

		return Task.FromResult(ItemsToReturn);
	}
}