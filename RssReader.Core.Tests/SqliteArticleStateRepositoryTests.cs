using Microsoft.Data.Sqlite;
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class SqliteArticleStateRepositoryTests
{
	[Fact]
	public async Task IsReadAsync_WhenArticleHasNoState_ReturnsFalse()
	{
		using var database = new TemporaryDatabase();
		SqliteArticleStateRepository repository =
				await CreateRepositoryAsync(database.Path);

		bool isRead = await repository.IsReadAsync(
				"https://example.com/feed",
				"article-123");

		Assert.False(isRead);
	}

	[Fact]
	public async Task MarkAsReadAsync_MakesArticleRead()
	{
		using var database = new TemporaryDatabase();
		SqliteArticleStateRepository repository =
				await CreateRepositoryAsync(database.Path);

		await repository.MarkAsReadAsync(
				"https://example.com/feed",
				"article-123");

		Assert.True(await repository.IsReadAsync(
				"https://example.com/feed",
				"article-123"));
	}

	[Fact]
	public async Task MarkAsUnreadAsync_MakesArticleUnread()
	{
		using var database = new TemporaryDatabase();
		SqliteArticleStateRepository repository =
				await CreateRepositoryAsync(database.Path);

		await repository.MarkAsReadAsync(
				"https://example.com/feed",
				"article-123");

		await repository.MarkAsUnreadAsync(
				"https://example.com/feed",
				"article-123");

		Assert.False(await repository.IsReadAsync(
				"https://example.com/feed",
				"article-123"));
	}

	[Fact]
	public async Task MarkAsReadAsync_CanBeCalledRepeatedly()
	{
		using var database = new TemporaryDatabase();
		SqliteArticleStateRepository repository =
				await CreateRepositoryAsync(database.Path);

		await repository.MarkAsReadAsync(
				"https://example.com/feed",
				"article-123");

		await repository.MarkAsReadAsync(
				"https://example.com/feed",
				"article-123");

		Assert.True(await repository.IsReadAsync(
				"https://example.com/feed",
				"article-123"));
	}

	[Fact]
	public async Task MarkAsUnreadAsync_CanBeCalledRepeatedly()
	{
		using var database = new TemporaryDatabase();
		SqliteArticleStateRepository repository =
				await CreateRepositoryAsync(database.Path);

		await repository.MarkAsUnreadAsync(
				"https://example.com/feed",
				"article-123");

		await repository.MarkAsUnreadAsync(
				"https://example.com/feed",
				"article-123");

		Assert.False(await repository.IsReadAsync(
				"https://example.com/feed",
				"article-123"));
	}

	[Fact]
	public async Task ReadState_PersistsAcrossRepositoryInstances()
	{
		using var database = new TemporaryDatabase();

		SqliteArticleStateRepository firstRepository =
				await CreateRepositoryAsync(database.Path);

		await firstRepository.MarkAsReadAsync(
				"https://example.com/feed",
				"article-123");

		SqliteArticleStateRepository secondRepository =
				await CreateRepositoryAsync(database.Path);

		Assert.True(await secondRepository.IsReadAsync(
				"https://example.com/feed",
				"article-123"));
	}

	[Fact]
	public async Task ReadState_IsIndependentBetweenArticlesAndFeeds()
	{
		using var database = new TemporaryDatabase();
		SqliteArticleStateRepository repository =
				await CreateRepositoryAsync(database.Path);

		await repository.MarkAsReadAsync(
				"https://example.com/feed-a",
				"article-123");

		Assert.True(await repository.IsReadAsync(
				"https://example.com/feed-a",
				"article-123"));

		Assert.False(await repository.IsReadAsync(
				"https://example.com/feed-b",
				"article-123"));

		Assert.False(await repository.IsReadAsync(
				"https://example.com/feed-a",
				"article-456"));
	}

	private static async Task<SqliteArticleStateRepository>
			CreateRepositoryAsync(string databasePath)
	{
		SqliteArticleStateRepository repository = new(databasePath);
		await repository.InitializeAsync();
		return repository;
	}

	private sealed class TemporaryDatabase : IDisposable
	{
		public string Path { get; } = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				$"RssReaderTests-{Guid.NewGuid():N}.db");

		public void Dispose()
		{
			SqliteConnection.ClearAllPools();

			if (File.Exists(Path))
			{
				File.Delete(Path);
			}
		}
	}

	[Fact]
	public async Task RemovingSubscription_DoesNotDeleteArticleReadState()
	{
		// Arrange
		using var database = new TemporaryDatabase();

		var subscriptionRepository =
				new SqliteSubscriptionRepository(database.Path);

		await subscriptionRepository.InitializeAsync();

		var articleStateRepository =
				await CreateRepositoryAsync(database.Path);

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		const string feedUrl = "https://example.com/feed.xml";
		const string articleId = "article-123";

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Example Feed"
		});

		await articleStateRepository.MarkAsReadAsync(
				feedUrl,
				articleId);

		// Act
		bool removed = await subscriptionService.RemoveAsync(feedUrl);

		// Assert
		Assert.True(removed);

		Assert.Empty(await subscriptionService.GetAllAsync());

		Assert.True(await articleStateRepository.IsReadAsync(
				feedUrl,
				articleId));
	}


	[Fact]
	public async Task MarkAsReadAsync_PersistsAcrossRepositoryInstances()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";
		const string articleId = "article-123";

		try
		{
			var firstRepository =
					new SqliteArticleStateRepository(databasePath);

			await firstRepository.InitializeAsync();

			Assert.False(
					await firstRepository.IsReadAsync(
							feedUrl,
							articleId));

			await firstRepository.MarkAsReadAsync(
					feedUrl,
					articleId);

			var reopenedRepository =
					new SqliteArticleStateRepository(databasePath);

			await reopenedRepository.InitializeAsync();

			Assert.True(
					await reopenedRepository.IsReadAsync(
							feedUrl,
							articleId));

			await reopenedRepository.MarkAsUnreadAsync(
					feedUrl,
					articleId);

			Assert.False(
					await firstRepository.IsReadAsync(
							feedUrl,
							articleId));
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
	public async Task ArticleContentChanges_PreserveReadState()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";

		try
		{
			var repository =
					new SqliteArticleStateRepository(databasePath);

			await repository.InitializeAsync();

			var articleStateService =
					new ArticleStateService(repository);

			var originalArticle = new FeedItem
			{
				Id = "article-123",
				FeedUrl = feedUrl,
				Link = "https://example.com/article-123",
				Title = "Original Title",
				Summary = "Original Summary",
				Content = "Original Content"
			};

			await articleStateService.MarkAsReadAsync(
					feedUrl,
					originalArticle);

			var updatedArticle = new FeedItem
			{
				Id = "article-123",
				FeedUrl = feedUrl,
				Link = "https://example.com/article-123",
				Title = "Updated Title",
				Summary = "Updated Summary",
				Content = "Updated Content"
			};

			var reopenedRepository =
					new SqliteArticleStateRepository(databasePath);

			await reopenedRepository.InitializeAsync();

			var reopenedService =
					new ArticleStateService(reopenedRepository);

			Assert.True(
					await reopenedService.IsReadAsync(
							feedUrl,
							updatedArticle));
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