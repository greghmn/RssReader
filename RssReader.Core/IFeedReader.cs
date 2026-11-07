
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
