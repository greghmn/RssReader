using Microsoft.Data.Sqlite;
using RssReader.Core;
using System.Reflection.Metadata;
using Xunit;

namespace RssReader.Core.Tests;

public class SqliteArticleRepositoryTests
{
	[Fact]
	public async Task SaveAsync_StoresArticleThatCanBeRetrieved()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"rss-reader-articles-{Guid.NewGuid():N}.db");

		try
		{
			var repository =
					new SqliteArticleRepository(databasePath);

			await repository.InitializeAsync();

			var publishedAt = new DateTimeOffset(
					2026, 11, 4, 12, 30, 0, TimeSpan.Zero);

			var article = new FeedItem
			{
				Id = "article-123",
				FeedUrl = "https://example.com/feed.xml",
				Link = "https://example.com/articles/123",
				Title = "An interesting article",
				Summary = "A short summary.",
				Content = "The complete article content.",
				PublishedAt = publishedAt
			};

			await repository.SaveAsync(
					article.FeedUrl,
					new[] { article });

			List<FeedItem> savedArticles =
					await repository.GetByFeedAsync(article.FeedUrl);

			Assert.Single(savedArticles);

			FeedItem saved = savedArticles[0];

			Assert.Equal(article.Id, saved.Id);
			Assert.Equal(article.FeedUrl, saved.FeedUrl);
			Assert.Equal(article.Link, saved.Link);
			Assert.Equal(article.Title, saved.Title);
			Assert.Equal(article.Summary, saved.Summary);
			Assert.Equal(article.Content, saved.Content);
			Assert.Equal(article.PublishedAt, saved.PublishedAt);
		}
		finally
		{
			SqliteConnection.ClearAllPools();  //tells Microsoft.Data.Sqlite to release pooled connections that are no longer in use

			if (File.Exists(databasePath))
			{
				File.Delete(databasePath);
			}
		}
	}

	//saves an article with the same Id but different content
	[Fact]
	public async Task SaveAsync_UpdatesExistingArticleWithoutDuplicating()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"rss-reader-articles-{Guid.NewGuid():N}.db");

		try
		{
			var repository = new SqliteArticleRepository(databasePath);
			await repository.InitializeAsync();

			const string feedUrl = "https://example.com/feed.xml";

			var original = new FeedItem
			{
				Id = "article-123",
				FeedUrl = feedUrl,
				Link = "https://example.com/articles/123",
				Title = "Original title",
				Summary = "Original summary",
				Content = "Original content"
			};

			var updated = new FeedItem
			{
				Id = "article-123",
				FeedUrl = feedUrl,
				Link = "https://example.com/articles/123",
				Title = "Corrected title",
				Summary = "Updated summary",
				Content = "Updated content"
			};

			await repository.SaveAsync(feedUrl, new[] { original });
			await repository.SaveAsync(feedUrl, new[] { updated });

			List<FeedItem> saved =
					await repository.GetByFeedAsync(feedUrl);

			Assert.Single(saved);
			Assert.Equal("Corrected title", saved[0].Title);
			Assert.Equal("Updated summary", saved[0].Summary);
			Assert.Equal("Updated content", saved[0].Content);
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

	//test articles with missing identifiers
	[Fact]
	public async Task SaveAsync_PreservesArticlesWithoutIdentifiers()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"rss-reader-articles-{Guid.NewGuid():N}.db");

		try
		{
			var repository = new SqliteArticleRepository(databasePath);
			await repository.InitializeAsync();

			const string feedUrl = "https://example.com/feed.xml";

			var article = new FeedItem
			{
				FeedUrl = feedUrl,
				Title = "An article without an identifier",
				Summary = "This article has no ID or link.",
				Content = "Some potentially valuable content."
			};

			await repository.SaveAsync(feedUrl, new[] { article });
			await repository.SaveAsync(feedUrl, new[] { article });

			List<FeedItem> saved =
					await repository.GetByFeedAsync(feedUrl);

			Assert.Equal(2, saved.Count);

			Assert.All(saved, item =>
			{
				Assert.Equal(article.Title, item.Title);
				Assert.Equal(article.Summary, item.Summary);
				Assert.Equal(article.Content, item.Content);
			});
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

	//test preservation across refreshes

	[Fact]
	public async Task SaveAsync_DoesNotDeleteArticlesMissingFromLaterBatch()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"rss-reader-articles-{Guid.NewGuid():N}.db");

		try
		{
			var repository = new SqliteArticleRepository(databasePath);
			await repository.InitializeAsync();

			const string feedUrl = "https://example.com/feed.xml";

			var first = new FeedItem
			{
				Id = "article-1",
				Title = "First article",
				FeedUrl = feedUrl
			};

			var second = new FeedItem
			{
				Id = "article-2",
				Title = "Second article",
				FeedUrl = feedUrl
			};

			var third = new FeedItem
			{
				Id = "article-3",
				Title = "Third article",
				FeedUrl = feedUrl
			};

			// Initial retrieval contains three articles.
			await repository.SaveAsync(
					feedUrl,
					new[] { first, second, third });

			// Later retrieval contains only the third.
			await repository.SaveAsync(
					feedUrl,
					new[] { third });

			List<FeedItem> saved =
					await repository.GetByFeedAsync(feedUrl);

			Assert.Equal(3, saved.Count);

			Assert.Contains(saved, item => item.Id == "article-1");
			Assert.Contains(saved, item => item.Id == "article-2");
			Assert.Contains(saved, item => item.Id == "article-3");
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

	//verify transaction rollback

	[Fact]
	public async Task SaveAsync_RollsBackEntireBatchWhenInsertFails()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"rss-reader-articles-{Guid.NewGuid():N}.db");

		try
		{
			var repository = new SqliteArticleRepository(databasePath);
			await repository.InitializeAsync();

			const string feedUrl = "https://example.com/feed.xml";

			var existing = new FeedItem
			{
				Id = "article-1",
				Title = "Original title",
				FeedUrl = feedUrl
			};

			await repository.SaveAsync(feedUrl, new[] { existing });

			var updated = new FeedItem
			{
				Id = "article-1",
				Title = "Updated title",
				FeedUrl = feedUrl
			};

			var invalid = new FeedItem
			{
				Id = "article-2",
				Title = null!,
				FeedUrl = feedUrl
			};

			await Assert.ThrowsAsync<InvalidOperationException>(
					() => repository.SaveAsync(
							feedUrl,
							new[] { updated, invalid }));

			List<FeedItem> saved =
					await repository.GetByFeedAsync(feedUrl);

			Assert.Single(saved);
			Assert.Equal("article-1", saved[0].Id);
			Assert.Equal("Original title", saved[0].Title);
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

	//verify that feeds remain independent

	[Fact]
	public async Task SaveAsync_KeepsArticlesFromDifferentFeedsSeparate()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"rss-reader-articles-{Guid.NewGuid():N}.db");

		try
		{
			var repository =
					new SqliteArticleRepository(databasePath);

			await repository.InitializeAsync();

			const string firstFeed = "https://example.com/feed.xml";
			const string secondFeed = "https://other.example.com/feed.xml";

			var firstArticle = new FeedItem
			{
				Id = "article-123",
				FeedUrl = firstFeed,
				Title = "First feed's article"
			};

			var secondArticle = new FeedItem
			{
				Id = "article-123",
				FeedUrl = secondFeed,
				Title = "Second feed's article"
			};

			await repository.SaveAsync(
					firstFeed, new[] { firstArticle });

			await repository.SaveAsync(
					secondFeed, new[] { secondArticle });

			List<FeedItem> firstResults =
					await repository.GetByFeedAsync(firstFeed);

			List<FeedItem> secondResults =
					await repository.GetByFeedAsync(secondFeed);

			Assert.Single(firstResults);
			Assert.Single(secondResults);

			Assert.Equal("First feed's article", firstResults[0].Title);
			Assert.Equal("Second feed's article", secondResults[0].Title);
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
	public async Task SaveAsync_WithEmptyFeed_PreservesExistingArticles()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		try
		{
			var repository =
					new SqliteArticleRepository(databasePath);

			await repository.InitializeAsync();

			const string feedUrl =
					"https://example.com/feed.xml";

			var originalArticle = new FeedItem
			{
				FeedUrl = feedUrl,
				Id = "article-1",
				Link = "https://example.com/article-1",
				Title = "Original Article",
				Summary = "Original summary",
				Content = "Original content"
			};

			await repository.SaveAsync(
					feedUrl,
					new[] { originalArticle });

			await repository.SaveAsync(
					feedUrl,
					Array.Empty<FeedItem>());

			List<FeedItem> stored =
					await repository.GetByFeedAsync(feedUrl);

			FeedItem article = Assert.Single(stored);

			Assert.Equal(originalArticle.Id, article.Id);
			Assert.Equal(originalArticle.Link, article.Link);
			Assert.Equal(originalArticle.Title, article.Title);
			Assert.Equal(originalArticle.Summary, article.Summary);
			Assert.Equal(originalArticle.Content, article.Content);
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
	public async Task SaveAsync_WhenBatchFails_RollsBackAllChanges()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";

		try
		{
			var repository =
					new SqliteArticleRepository(databasePath);

			await repository.InitializeAsync();

			await repository.SaveAsync(
					feedUrl,
					new[]
					{
								new FeedItem
								{
										Id = "existing",
										Link = "https://example.com/existing",
										Title = "Original Title",
										Summary = "Original summary",
										Content = "Original content",
										FeedUrl = feedUrl
								}
					});

			var batch = new[]
			{
						new FeedItem
						{
								Id = "existing",
								Link = "https://example.com/existing",
								Title = "Updated Title",
								Summary = "Updated summary",
								Content = "Updated content",
								FeedUrl = feedUrl
						},
						new FeedItem
						{
								Id = "new",
								Link = "https://example.com/new",
								Title = "New Article",
								Summary = "New summary",
								Content = "New content",
								FeedUrl = feedUrl
						},
						new FeedItem
						{
								Id = "invalid",
								Link = "https://example.com/invalid",
								Title = null!,
								Summary = "Invalid summary",
								Content = "Invalid content",
								FeedUrl = feedUrl
						}
				};

			await Assert.ThrowsAnyAsync<Exception>(
					() => repository.SaveAsync(feedUrl, batch));

			List<FeedItem> stored =
					await repository.GetByFeedAsync(feedUrl);

			FeedItem remaining = Assert.Single(stored);

			Assert.Equal("existing", remaining.Id);
			Assert.Equal("Original Title", remaining.Title);
			Assert.Equal("Original summary", remaining.Summary);
			Assert.Equal("Original content", remaining.Content);
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
	public async Task SaveAsync_WhenFeedChanges_PreservesHistoricalArticles()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";

		try
		{
			var repository =
					new SqliteArticleRepository(databasePath);

			await repository.InitializeAsync();

			await repository.SaveAsync(
					feedUrl,
					new[]
					{
								new FeedItem
								{
										Id = "article-a",
										Link = "https://example.com/a",
										Title = "Article A",
										Summary = "Original A",
										Content = "Content A",
										FeedUrl = feedUrl
								},
								new FeedItem
								{
										Id = "article-b",
										Link = "https://example.com/b",
										Title = "Article B",
										Summary = "Original B",
										Content = "Content B",
										FeedUrl = feedUrl
								}
					});

			await repository.SaveAsync(
					feedUrl,
					new[]
					{
								new FeedItem
								{
										Id = "article-b",
										Link = "https://example.com/b",
										Title = "Updated Article B",
										Summary = "Updated B",
										Content = "Updated Content B",
										FeedUrl = feedUrl
								},
								new FeedItem
								{
										Id = "article-c",
										Link = "https://example.com/c",
										Title = "Article C",
										Summary = "New C",
										Content = "Content C",
										FeedUrl = feedUrl
								}
					});

			List<FeedItem> stored =
					await repository.GetByFeedAsync(feedUrl);

			Assert.Equal(3, stored.Count);

			FeedItem articleA = Assert.Single(
					stored,
					article => article.Id == "article-a");

			FeedItem articleB = Assert.Single(
					stored,
					article => article.Id == "article-b");

			FeedItem articleC = Assert.Single(
					stored,
					article => article.Id == "article-c");

			Assert.Equal("Original A", articleA.Summary);
			Assert.Equal("Updated Article B", articleB.Title);
			Assert.Equal("Updated B", articleB.Summary);
			Assert.Equal("Article C", articleC.Title);
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