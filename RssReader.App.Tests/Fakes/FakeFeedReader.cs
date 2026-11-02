using RssReader.Core;

namespace RssReader.App.Tests.Fakes;

public class FakeFeedReader : IFeedReader
{
	public List<FeedItem> Items { get; set; } = new();

	public Exception? ReadException { get; set; }

	public Task<List<FeedItem>> ReadAsync(
			string feedUrl,
			CancellationToken cancellationToken = default)
	{
		if (ReadException is not null)
		{
			throw ReadException;
		}

		return Task.FromResult(Items.ToList());
	}
}