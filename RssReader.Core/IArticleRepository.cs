namespace RssReader.Core;

public interface IArticleRepository
{
	Task<List<FeedItem>> GetByFeedAsync(string feedUrl);

	Task SaveAsync(
			string feedUrl,
			IEnumerable<FeedItem> articles);
}