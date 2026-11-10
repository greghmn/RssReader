using Microsoft.Data.Sqlite;
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class SqliteArticleRepositoryMigrationTests
{
	[Fact]
	public async Task InitializeAsync_AddsRawPublishedDateToExistingDatabase()
	{
		string databasePath = Path.Combine(
						Path.GetTempPath(),
						$"rss-reader-migration-{Guid.NewGuid():N}.db");

		try
		{
			await using (var connection = new SqliteConnection(
							$"Data Source={databasePath}"))
			{
				await connection.OpenAsync();

				await using var command = connection.CreateCommand();

				command.CommandText = """
                CREATE TABLE Articles (
                    StorageId INTEGER PRIMARY KEY,
                    FeedUrl TEXT NOT NULL,
                    ArticleId TEXT NULL,
                    Link TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Summary TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    PublishedAt TEXT NULL,
                    IdentityKey TEXT NULL
                );

                INSERT INTO Articles (
                    FeedUrl, ArticleId, Link, Title,
                    Summary, Content, PublishedAt, IdentityKey
                )
                VALUES (
                    'https://example.com/feed.xml',
                    'article-1',
                    'https://example.com/article-1',
                    'Existing Article',
                    'Existing summary',
                    'Existing content',
                    '2026-11-09T23:02:07.0000000Z',
                    'article-1'
                );
                """;

				await command.ExecuteNonQueryAsync();
			}

			var repository = new SqliteArticleRepository(databasePath);

			await repository.InitializeAsync();

			//this second call should detect that RawPublishedDate exists and skip ALTER TABLE 

			await repository.InitializeAsync();

			await using var verification =
							new SqliteConnection($"Data Source={databasePath}");

			await verification.OpenAsync();

			await using var check = verification.CreateCommand();

			check.CommandText = """
            SELECT RawPublishedDate
            FROM Articles
            WHERE ArticleId = 'article-1';
            """;

			object? value = await check.ExecuteScalarAsync();

			Assert.Equal(DBNull.Value, value);

			List<FeedItem> articles = await repository.GetByFeedAsync(
							"https://example.com/feed.xml");

			FeedItem article = Assert.Single(articles);

			Assert.Equal("article-1", article.Id);
			Assert.Equal("Existing Article", article.Title);
			Assert.Equal("Existing summary", article.Summary);
			Assert.Equal("Existing content", article.Content);
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
