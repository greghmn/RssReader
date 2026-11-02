using RssReader.Core;

namespace RssReader.App.Tests.Fakes;

public class FakeArticleStateRepository : IArticleStateRepository
{
	private readonly HashSet<(string FeedUrl, string ArticleId)> _readArticles = new();

	public Exception? MarkAsReadException { get; set; }

	public Exception? MarkAsUnreadException { get; set; }

	public Task<bool> IsReadAsync(string feedUrl, string articleId)
	{
		return Task.FromResult(
				_readArticles.Contains((feedUrl, articleId)));
	}

	public Task MarkAsReadAsync(string feedUrl, string articleId)
	{
		if (MarkAsReadException is not null)
		{
			throw MarkAsReadException;
		}

		_readArticles.Add((feedUrl, articleId));

		return Task.CompletedTask;
	}

	public Task MarkAsUnreadAsync(string feedUrl, string articleId)
	{
		if (MarkAsUnreadException is not null)
		{
			throw MarkAsUnreadException;
		}

		_readArticles.Remove((feedUrl, articleId));

		return Task.CompletedTask;
	}
}