//read-state table and subscriptions table are independent

using Microsoft.Data.Sqlite;

namespace RssReader.Core;

public class SqliteArticleStateRepository : IArticleStateRepository
{
    private readonly string _connectionString;

    public SqliteArticleStateRepository(string databasePath)
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
            CREATE TABLE IF NOT EXISTS ArticleReadState (
                FeedUrl TEXT NOT NULL,
                ArticleId TEXT NOT NULL,
                IsRead INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (FeedUrl, ArticleId)
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsReadAsync(
        string feedUrl,
        string articleId)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM ArticleReadState
                WHERE FeedUrl = $feedUrl
                  AND ArticleId = $articleId
                  AND IsRead = 1
            );
            """;

        command.Parameters.AddWithValue("$feedUrl", feedUrl);
        command.Parameters.AddWithValue("$articleId", articleId);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt64(result) == 1;
    }

    public async Task MarkAsReadAsync(
        string feedUrl,
        string articleId)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO ArticleReadState (FeedUrl, ArticleId, IsRead)
            VALUES ($feedUrl, $articleId, 1)
            ON CONFLICT (FeedUrl, ArticleId)
            DO UPDATE SET IsRead = 1;
            """;

        command.Parameters.AddWithValue("$feedUrl", feedUrl);
        command.Parameters.AddWithValue("$articleId", articleId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task MarkAsUnreadAsync(
        string feedUrl,
        string articleId)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM ArticleReadState
            WHERE FeedUrl = $feedUrl
              AND ArticleId = $articleId;
            """;

        command.Parameters.AddWithValue("$feedUrl", feedUrl);
        command.Parameters.AddWithValue("$articleId", articleId);

        await command.ExecuteNonQueryAsync();
    }
}
