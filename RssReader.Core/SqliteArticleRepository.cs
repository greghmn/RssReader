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
        IdentityKey TEXT NULL
    );

    CREATE UNIQUE INDEX IF NOT EXISTS
        IX_Articles_FeedUrl_IdentityKey
    ON Articles (FeedUrl, IdentityKey)
    WHERE IdentityKey IS NOT NULL;
    """;

		await command.ExecuteNonQueryAsync();
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
               Content, PublishedAt, FeedUrl
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

				FeedUrl = reader.GetString(6)
			});
		}

		return articles;
	}

	//SQLite transaction so batch succeeds or fails as a whole 

	public async Task SaveAsync(
		string feedUrl,
		IEnumerable<FeedItem> articles)
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
                    $identityKey
                )
                ON CONFLICT (FeedUrl, IdentityKey)
                WHERE IdentityKey IS NOT NULL
                DO UPDATE SET
                    ArticleId = excluded.ArticleId,
                    Link = excluded.Link,
                    Title = excluded.Title,
                    Summary = excluded.Summary,
                    Content = excluded.Content,
                    PublishedAt = excluded.PublishedAt;
                """;

				command.Parameters.AddWithValue("$feedUrl", feedUrl);

				command.Parameters.AddWithValue(
						"$articleId",
						string.IsNullOrWhiteSpace(article.Id)
								? DBNull.Value
								: article.Id);

				command.Parameters.AddWithValue("$link", article.Link);
				command.Parameters.AddWithValue("$title", article.Title);
				command.Parameters.AddWithValue("$summary", article.Summary);
				command.Parameters.AddWithValue("$content", article.Content);

				command.Parameters.AddWithValue(
						"$publishedAt",
						article.PublishedAt.HasValue
								? article.PublishedAt.Value
										.ToUniversalTime()
										.ToString("O")
								: DBNull.Value);

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