using RssReader.Core;

namespace RssReader.App.Tests.Fakes;

public class FakeFeedReader : IFeedReader
{
	public List<FeedItem> Items { get; set; } = new();

	public Exception? ReadException { get; set; }

	public TaskCompletionSource<bool>? ReadStarted { get; set; }

	public TaskCompletionSource<bool>? AllowRead { get; set; }

	public Func<string, CancellationToken, Task<List<FeedItem>>>? ReadHandler { get; set; }

	public async Task<List<FeedItem>> ReadAsync(string feedUrl, CancellationToken cancellationToken = default)
	{
		if (ReadHandler is not null)
		{
			return await ReadHandler(feedUrl, cancellationToken);
		}
		ReadStarted?.TrySetResult(true);

		if (AllowRead is not null)
		{
			await AllowRead.Task.WaitAsync(cancellationToken);
		}

		if (ReadException is not null)
		{
			throw ReadException;
		}

		return Items.ToList();
	}
}