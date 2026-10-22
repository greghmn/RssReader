
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
}
