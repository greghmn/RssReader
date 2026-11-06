
using Microsoft.Data.Sqlite;
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class SubscriptionRemovalPersistenceTests
{
	[Fact]
	public async Task RemoveSubscription_PreservesArticlesAndReadState()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";

		try
		{
			var subscriptionRepository =
					new SqliteSubscriptionRepository(databasePath);

			var articleRepository =
					new SqliteArticleRepository(databasePath);

			var stateRepository =
					new SqliteArticleStateRepository(databasePath);

			await subscriptionRepository.InitializeAsync();
			await articleRepository.InitializeAsync();
			await stateRepository.InitializeAsync();

			var subscriptionService =
					new SubscriptionService(subscriptionRepository);

			var articleStateService =
					new ArticleStateService(stateRepository);

			await subscriptionService.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Example Feed"
			});

			var article = new FeedItem
			{
				Id = "article-123",
				FeedUrl = feedUrl,
				Link = "https://example.com/article-123",
				Title = "Archived Article",
				Summary = "Archived summary",
				Content = "Archived content"
			};

			await articleRepository.SaveAsync(
					feedUrl,
					new[] { article });

			await articleStateService.MarkAsReadAsync(
					feedUrl,
					article);

			bool removed =
					await subscriptionService.RemoveAsync(feedUrl);

			Assert.True(removed);

			Assert.False(
					await subscriptionService.IsSubscribedAsync(feedUrl));

			var reopenedArticleRepository =
					new SqliteArticleRepository(databasePath);

			var reopenedStateRepository =
					new SqliteArticleStateRepository(databasePath);

			await reopenedArticleRepository.InitializeAsync();
			await reopenedStateRepository.InitializeAsync();

			List<FeedItem> storedArticles =
					await reopenedArticleRepository.GetByFeedAsync(feedUrl);

			FeedItem storedArticle = Assert.Single(storedArticles);

			Assert.Equal(article.Id, storedArticle.Id);
			Assert.Equal(article.Title, storedArticle.Title);

			var reopenedStateService =
					new ArticleStateService(reopenedStateRepository);

			Assert.True(
					await reopenedStateService.IsReadAsync(
							feedUrl,
							storedArticle));
		}
		finally
		{
			SqliteConnection.ClearAllPools();

			if (File.Exists(databasePath))
			{
				File.Delete(databasePath);
			}
		}
	}


	[Fact]
	public async Task Resubscribing_RestoresAccessToArchivedArticlesAndReadState()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";

		try
		{
			var subscriptionRepository =
					new SqliteSubscriptionRepository(databasePath);

			var articleRepository =
					new SqliteArticleRepository(databasePath);

			var stateRepository =
					new SqliteArticleStateRepository(databasePath);

			await subscriptionRepository.InitializeAsync();
			await articleRepository.InitializeAsync();
			await stateRepository.InitializeAsync();

			var subscriptionService =
					new SubscriptionService(subscriptionRepository);

			var articleStateService =
					new ArticleStateService(stateRepository);

			await subscriptionService.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Original Subscription"
			});

			var article = new FeedItem
			{
				Id = "article-123",
				FeedUrl = feedUrl,
				Link = "https://example.com/article-123",
				Title = "Archived Article",
				Summary = "Archived summary",
				Content = "Archived content"
			};

			await articleRepository.SaveAsync(
					feedUrl,
					new[] { article });

			await articleStateService.MarkAsReadAsync(
					feedUrl,
					article);

			Assert.True(
					await subscriptionService.RemoveAsync(feedUrl));

			await subscriptionService.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Restored Subscription"
			});

			Assert.True(
					await subscriptionService.IsSubscribedAsync(feedUrl));

			var reopenedArticleRepository =
					new SqliteArticleRepository(databasePath);

			var reopenedStateRepository =
					new SqliteArticleStateRepository(databasePath);

			await reopenedArticleRepository.InitializeAsync();
			await reopenedStateRepository.InitializeAsync();

			List<FeedItem> stored =
					await reopenedArticleRepository.GetByFeedAsync(feedUrl);

			FeedItem restoredArticle = Assert.Single(stored);

			Assert.Equal(article.Id, restoredArticle.Id);
			Assert.Equal(article.Title, restoredArticle.Title);

			var reopenedStateService =
					new ArticleStateService(reopenedStateRepository);

			Assert.True(
					await reopenedStateService.IsReadAsync(
							feedUrl,
							restoredArticle));
		}
		finally
		{
			SqliteConnection.ClearAllPools();

			if (File.Exists(databasePath))
			{
				File.Delete(databasePath);
			}
		}
	}

}
