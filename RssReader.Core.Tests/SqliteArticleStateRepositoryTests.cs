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
}