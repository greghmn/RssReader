using RssReader.Core;

namespace RssReader.Core.Tests;

public class InMemoryArticleRepository : IArticleRepository
{
	private readonly List<FeedItem> _articles = new();

	public Task<List<FeedItem>> GetByFeedAsync(string feedUrl)
	{
		List<FeedItem> articles = _articles
				.Where(article => article.FeedUrl == feedUrl)
				.ToList();

		return Task.FromResult(articles);
	}

	public Task SaveAsync(
			string feedUrl,
			IEnumerable<FeedItem> articles)
	{
		foreach (FeedItem article in articles)
		{
			_articles.Add(new FeedItem
			{
				Id = article.Id,
				FeedUrl = feedUrl,
				Link = article.Link,
				Title = article.Title,
				Summary = article.Summary,
				Content = article.Content,
				PublishedAt = article.PublishedAt
			});
		}

		return Task.CompletedTask;
	}


}