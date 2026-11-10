// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using Microsoft.Data.Sqlite;

namespace RssReader.Core;

public class SqliteArticleRepository : IArticleRepository
{
	private readonly string _connectionString;

	public SqliteArticleRepository(string databasePath)
	{
		_connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = databasePath
		}.ToString();
	}


	public async Task InitializeAsync()
	{
		await using var connection =
						new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
        CREATE TABLE IF NOT EXISTS Articles (
            StorageId INTEGER PRIMARY KEY,
            FeedUrl TEXT NOT NULL,
            ArticleId TEXT NULL,
            Link TEXT NOT NULL,
            Title TEXT NOT NULL,
            Summary TEXT NOT NULL,
            Content TEXT NOT NULL,
            PublishedAt TEXT NULL,
            RawPublishedDate TEXT NULL,
            IdentityKey TEXT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS
            IX_Articles_FeedUrl_IdentityKey
        ON Articles (FeedUrl, IdentityKey)
        WHERE IdentityKey IS NOT NULL;
        """;

		await command.ExecuteNonQueryAsync();

		command.CommandText = "PRAGMA table_info(Articles);";

		bool hasRawPublishedDate = false;

		await using (var reader = await command.ExecuteReaderAsync())
		{
			while (await reader.ReadAsync())
			{
				if (reader.GetString(1) == "RawPublishedDate")
				{
					hasRawPublishedDate = true;
					break;
				}
			}
		}

		if (!hasRawPublishedDate)
		{
			command.CommandText = """
            ALTER TABLE Articles
            ADD COLUMN RawPublishedDate TEXT NULL;
            """;

			await command.ExecuteNonQueryAsync();
		}
	}


	//retrieve all locally stored articles belonging to a particular feed, without contacting the network


	public async Task<List<FeedItem>> GetByFeedAsync(string feedUrl)
	{
		var articles = new List<FeedItem>();

		await using var connection =
						new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
        SELECT ArticleId, Link, Title, Summary,
               Content, PublishedAt, FeedUrl, RawPublishedDate
        FROM Articles
        WHERE FeedUrl = $feedUrl
        ORDER BY PublishedAt DESC, StorageId DESC;
        """;

		command.Parameters.AddWithValue("$feedUrl", feedUrl);

		await using var reader = await command.ExecuteReaderAsync();

		while (await reader.ReadAsync())
		{
			articles.Add(new FeedItem
			{
				Id = reader.IsDBNull(0)
									? ""
									: reader.GetString(0),

				Link = reader.GetString(1),
				Title = reader.GetString(2),
				Summary = reader.GetString(3),
				Content = reader.GetString(4),

				PublishedAt = reader.IsDBNull(5)
									? null
									: DateTimeOffset.Parse(reader.GetString(5)),

				FeedUrl = reader.GetString(6),

				RawPublishedDate = reader.IsDBNull(7)
									? null
									: reader.GetString(7)
			});
		}

		return articles;
	}


	//SQLite transaction so batch succeeds or fails as a whole 


	public async Task SaveAsync(string feedUrl, IEnumerable<FeedItem> articles)
	{
		await using var connection =
						new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var transaction =
						await connection.BeginTransactionAsync();

		try
		{
			foreach (FeedItem article in articles)
			{
				string? identityKey = ArticleIdentity.GetId(article);

				await using var command = connection.CreateCommand();

				command.Transaction = (SqliteTransaction)transaction;

				command.CommandText = """
                INSERT INTO Articles (
                    FeedUrl,
                    ArticleId,
                    Link,
                    Title,
                    Summary,
                    Content,
                    PublishedAt,
                    RawPublishedDate,
                    IdentityKey
                )
                VALUES (
                    $feedUrl,
                    $articleId,
                    $link,
                    $title,
                    $summary,
                    $content,
                    $publishedAt,
                    $rawPublishedDate,
                    $identityKey
                )
                ON CONFLICT (FeedUrl, IdentityKey)
                WHERE IdentityKey IS NOT NULL
                DO UPDATE SET
                    ArticleId = excluded.ArticleId,
                Link = COALESCE(
                    NULLIF(excluded.Link, ''),
                    Articles.Link
                ),
                Title = COALESCE(
                    NULLIF(excluded.Title, ''),
                    Articles.Title
                ),
                Summary = COALESCE(
                    NULLIF(excluded.Summary, ''),
                    Articles.Summary
                ),
                Content = COALESCE(
                    NULLIF(excluded.Content, ''),
                    Articles.Content
                ),
                    PublishedAt = COALESCE(
                    excluded.PublishedAt,
                    Articles.PublishedAt
                ),
                    RawPublishedDate = COALESCE(
                        excluded.RawPublishedDate,
                        Articles.RawPublishedDate
                    );
                """;

				command.Parameters.AddWithValue("$feedUrl", feedUrl);

				command.Parameters.AddWithValue(
								"$articleId",
								string.IsNullOrWhiteSpace(article.Id)
												? DBNull.Value
												: article.Id);

				command.Parameters.AddWithValue(
								"$link",
								string.IsNullOrWhiteSpace(article.Link)
												? ""
												: article.Link);
				command.Parameters.AddWithValue(
								"$title",
								string.IsNullOrWhiteSpace(article.Title)
												? ""
												: article.Title);
				command.Parameters.AddWithValue(
								"$summary",
								string.IsNullOrWhiteSpace(article.Summary)
												? ""
												: article.Summary);
				command.Parameters.AddWithValue(
								"$content",
								string.IsNullOrWhiteSpace(article.Content)
												? ""
												: article.Content);

				command.Parameters.AddWithValue(
								"$publishedAt",
								article.PublishedAt.HasValue
												? article.PublishedAt.Value
																.ToUniversalTime()
																.ToString("O")
												: DBNull.Value);

				command.Parameters.AddWithValue(
								"$rawPublishedDate",
								article.RawPublishedDate is null
												? DBNull.Value
												: article.RawPublishedDate);

				command.Parameters.AddWithValue(
								"$identityKey",
								identityKey is null
												? DBNull.Value
												: identityKey);

				await command.ExecuteNonQueryAsync();
			}

			await transaction.CommitAsync();
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

}
