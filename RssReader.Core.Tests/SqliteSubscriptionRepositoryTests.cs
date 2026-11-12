// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using RssReader.Core;
using Microsoft.Data.Sqlite;

namespace RssReader.Core.Tests;

public class SqliteSubscriptionRepositoryTests
{
	private static string CreateDatabasePath()
	{
		return Path.Combine(
				Path.GetTempPath(),
				$"RssReaderTests_{Guid.NewGuid():N}.db");
	}

	[Fact]
	public async Task AddAsync_Subscription_CanBeRetrieved()
	{
		// Arrange
		string databasePath = CreateDatabasePath();

		try
		{
			var repository =
					new SqliteSubscriptionRepository(databasePath);

			await repository.InitializeAsync();

			var subscription = new FeedSubscription
			{
				Url = "https://example.com/feed.xml",
				Title = "Example Feed",
				DateAdded = DateTimeOffset.UtcNow
			};

			// Act
			await repository.AddAsync(subscription);
			List<FeedSubscription> results =
					await repository.GetAllAsync();

			// Assert
			FeedSubscription result = Assert.Single(results);

			Assert.Equal(subscription.Url, result.Url);
			Assert.Equal(subscription.Title, result.Title);
			Assert.Equal(subscription.DateAdded, result.DateAdded);
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
	public async Task RemoveAsync_ExistingSubscription_RemovesIt()
	{
		// Arrange
		string databasePath = CreateDatabasePath();

		try
		{
			var repository =
					new SqliteSubscriptionRepository(databasePath);

			await repository.InitializeAsync();

			await repository.AddAsync(new FeedSubscription
			{
				Url = "https://example.com/feed.xml",
				Title = "Example Feed"
			});

			// Act
			bool removed = await repository.RemoveAsync(
					"https://example.com/feed.xml");

			List<FeedSubscription> results =
					await repository.GetAllAsync();

			// Assert
			Assert.True(removed);
			Assert.Empty(results);
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
	public async Task AddAsync_NewRepository_CanRetrieveSavedSubscription()
	{
		// Arrange
		string databasePath = CreateDatabasePath();

		try
		{
			var firstRepository =
					new SqliteSubscriptionRepository(databasePath);

			await firstRepository.InitializeAsync();

			await firstRepository.AddAsync(new FeedSubscription
			{
				Url = "https://example.com/feed.xml",
				Title = "Example Feed"
			});

			// Act
			var secondRepository =
					new SqliteSubscriptionRepository(databasePath);

			await secondRepository.InitializeAsync();

			List<FeedSubscription> results =
					await secondRepository.GetAllAsync();

			// Assert
			FeedSubscription result = Assert.Single(results);

			Assert.Equal(
					"https://example.com/feed.xml",
					result.Url);

			Assert.Equal("Example Feed", result.Title);
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
	public async Task RemoveAsync_NonexistentSubscription_ReturnsFalse()
	{
		// Arrange
		string databasePath = CreateDatabasePath();

		try
		{
			var repository =
					new SqliteSubscriptionRepository(databasePath);

			await repository.InitializeAsync();

			// Act
			bool removed = await repository.RemoveAsync(
					"https://example.com/missing-feed.xml");

			// Assert
			Assert.False(removed);
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
	public async Task GetAllAsync_MultipleSubscriptions_ReturnsThemInDateAddedOrder()
	{
		// Arrange
		string databasePath = CreateDatabasePath();

		try
		{
			var repository =
					new SqliteSubscriptionRepository(databasePath);

			await repository.InitializeAsync();

			var newer = new FeedSubscription
			{
				Url = "https://example.com/newer.xml",
				Title = "Newer Feed",
				DateAdded = new DateTimeOffset(
							2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
			};

			var older = new FeedSubscription
			{
				Url = "https://example.com/older.xml",
				Title = "Older Feed",
				DateAdded = new DateTimeOffset(
							2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
			};

			// Insert the newer subscription first to ensure the
			// query orders by date, not insertion order.
			await repository.AddAsync(newer);
			await repository.AddAsync(older);

			// Act
			List<FeedSubscription> results =
					await repository.GetAllAsync();

			// Assert
			Assert.Equal(2, results.Count);
			Assert.Equal("Older Feed", results[0].Title);
			Assert.Equal("Newer Feed", results[1].Title);
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
	public async Task GetAllAsync_NewlyInitializedRepository_ReturnsEmptyList()
	{
		// Arrange
		string databasePath = CreateDatabasePath();

		try
		{
			var repository =
					new SqliteSubscriptionRepository(databasePath);

			await repository.InitializeAsync();

			// Act
			List<FeedSubscription> results =
					await repository.GetAllAsync();

			// Assert
			Assert.Empty(results);
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
	public async Task RemoveAsync_PreservesPreviouslySavedArticles()
	{
		const string feedUrl = "https://example.com/feed.xml";

		string databasePath = CreateDatabasePath();

		try
		{
			var subscriptions =
							new SqliteSubscriptionRepository(databasePath);

			var articles =
							new SqliteArticleRepository(databasePath);

			await subscriptions.InitializeAsync();
			await articles.InitializeAsync();

			await subscriptions.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Example Feed"
			});

			await articles.SaveAsync(feedUrl, new[]
			{
						new FeedItem
						{
								Id = "article-123",
								FeedUrl = feedUrl,
								Title = "An article worth preserving",
								Content = "This content should survive unsubscription."
						}
				});

			bool removed = await subscriptions.RemoveAsync(feedUrl);

			Assert.True(removed);
			Assert.Empty(await subscriptions.GetAllAsync());

			// Use a new repository instance to verify persisted data.
			var reopenedArticles =
							new SqliteArticleRepository(databasePath);

			List<FeedItem> saved =
							await reopenedArticles.GetByFeedAsync(feedUrl);

			FeedItem preserved = Assert.Single(saved);

			Assert.Equal("article-123", preserved.Id);
			Assert.Equal("An article worth preserving", preserved.Title);
			Assert.Equal(
							"This content should survive unsubscription.",
							preserved.Content);
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
	public async Task RemoveAsync_AndResubscribe_PreservesArticleReadState()
	{
		const string feedUrl = "https://example.com/feed.xml";
		const string articleId = "article-123";

		string databasePath = CreateDatabasePath();

		try
		{
			var subscriptions =
							new SqliteSubscriptionRepository(databasePath);

			var readStates =
							new SqliteArticleStateRepository(databasePath);

			await subscriptions.InitializeAsync();
			await readStates.InitializeAsync();

			await subscriptions.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Example Feed"
			});

			await readStates.MarkAsReadAsync(feedUrl, articleId);

			Assert.True(await readStates.IsReadAsync(feedUrl, articleId));

			// Remove the subscription, then add it again.
			bool removed = await subscriptions.RemoveAsync(feedUrl);

			Assert.True(removed);

			await subscriptions.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Restored Feed"
			});

			// Reopen through a separate repository instance.
			var reopenedReadStates =
							new SqliteArticleStateRepository(databasePath);

			bool isRead =
							await reopenedReadStates.IsReadAsync(feedUrl, articleId);

			Assert.True(isRead);
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
	public async Task InitializeAsync_MigratesLegacySchemaWithoutLosingSubscriptions()
	{
		string databasePath = CreateDatabasePath();

		const string feedUrl = "https://example.com/legacy.xml";
		const string dateAdded = "2026-01-01T12:00:00+00:00";

		try
		{
			// Simulate a database created by an older application version.
			await using (var connection =
							new SqliteConnection($"Data Source={databasePath}"))
			{
				await connection.OpenAsync();

				await using var command = connection.CreateCommand();

				command.CommandText = """
                CREATE TABLE Subscriptions (
                    Url TEXT NOT NULL PRIMARY KEY,
                    Title TEXT NOT NULL,
                    DateAdded TEXT NOT NULL
                );

                INSERT INTO Subscriptions (Url, Title, DateAdded)
                VALUES ($url, $title, $dateAdded);
                """;

				command.Parameters.AddWithValue("$url", feedUrl);
				command.Parameters.AddWithValue("$title", "Legacy Feed");
				command.Parameters.AddWithValue("$dateAdded", dateAdded);

				await command.ExecuteNonQueryAsync();
			}

			var repository =
							new SqliteSubscriptionRepository(databasePath);

			// First initialization performs the migration.
			await repository.InitializeAsync();

			// Second initialization must not attempt it again.
			await repository.InitializeAsync();

			List<FeedSubscription> subscriptions =
							await repository.GetAllAsync();

			FeedSubscription restored = Assert.Single(subscriptions);

			Assert.Equal(feedUrl, restored.Url);
			Assert.Equal("Legacy Feed", restored.Title);
			Assert.Equal(DateTimeOffset.Parse(dateAdded), restored.DateAdded);
			Assert.Null(restored.LastRetrieved);
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
	public async Task InitializeAsync_PreservesExistingLastRetrieved()
	{
		string databasePath = CreateDatabasePath();

		const string feedUrl = "https://example.com/feed.xml";

		var lastRetrieved = new DateTimeOffset(
						2026, 11, 10, 18, 30, 45,
						TimeSpan.FromHours(-8));

		try
		{
			var firstRepository =
							new SqliteSubscriptionRepository(databasePath);

			await firstRepository.InitializeAsync();

			await firstRepository.AddAsync(new FeedSubscription
			{
				Url = feedUrl,
				Title = "Example Feed"
			});

			bool updated = await firstRepository.UpdateLastRetrievedAsync(
							feedUrl, lastRetrieved);

			Assert.True(updated);

			// Simulate a later application startup.
			var secondRepository =
							new SqliteSubscriptionRepository(databasePath);

			await secondRepository.InitializeAsync();

			List<FeedSubscription> subscriptions =
							await secondRepository.GetAllAsync();

			FeedSubscription restored = Assert.Single(subscriptions);

			Assert.Equal(feedUrl, restored.Url);
			Assert.Equal(lastRetrieved, restored.LastRetrieved);
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
