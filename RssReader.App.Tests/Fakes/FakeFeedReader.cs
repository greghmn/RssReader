using RssReader.Core;
using RssReader.App.Results;

namespace RssReader.App.Tests.Fakes;

public class FakeFeedReader : IFeedReader
{
	public List<FeedItem> Items { get; set; } = new();

	public Exception? ReadException { get; set; }

	public TaskCompletionSource<bool>? ReadStarted { get; set; }

	public TaskCompletionSource<bool>? AllowRead { get; set; }

	public Func<string, CancellationToken, Task<List<FeedItem>>>? ReadHandler { get; set; }

	public string? TitleToReturn { get; set; }

	public Exception? TitleException { get; set; }

	public int TitleReadCount { get; private set; }

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