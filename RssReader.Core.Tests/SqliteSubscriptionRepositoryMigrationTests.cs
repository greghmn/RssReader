// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using Microsoft.Data.Sqlite;
using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class SqliteSubscriptionRepositoryMigrationTests
{
	[Fact]
	public async Task InitializeAsync_MigratesOldSchemaWithoutLosingSubscriptions()
	{
		string databasePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		const string feedUrl = "https://example.com/feed.xml";

		try
		{
			var connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = databasePath
			}.ToString();

			// Simulate a database created by an older application version.
			await using (var connection =
					new SqliteConnection(connectionString))
			{
				await connection.OpenAsync();

				await using var command = connection.CreateCommand();

				command.CommandText = """
                    CREATE TABLE Subscriptions (
                        Url TEXT NOT NULL PRIMARY KEY,
                        Title TEXT NOT NULL,
                        DateAdded TEXT NOT NULL
                    );

                    INSERT INTO Subscriptions (
                        Url, Title, DateAdded
                    )
                    VALUES (
                        'https://example.com/feed.xml',
                        'Existing Feed',
                        '2026-01-01T00:00:00+00:00'
                    );
                    """;

				await command.ExecuteNonQueryAsync();
			}

			var repository =
					new SqliteSubscriptionRepository(databasePath);

			await repository.InitializeAsync();

			// Initialization should also be safe to repeat.
			await repository.InitializeAsync();

			List<FeedSubscription> subscriptions =
					await repository.GetAllAsync();

			FeedSubscription subscription =
					Assert.Single(subscriptions);

			Assert.Equal(feedUrl, subscription.Url);
			Assert.Equal("Existing Feed", subscription.Title);
			Assert.Null(subscription.LastRetrieved);

			DateTimeOffset retrievedAt =
					new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

			bool updated =
					await repository.UpdateLastRetrievedAsync(
							feedUrl,
							retrievedAt);

			Assert.True(updated);

			var reopenedRepository =
					new SqliteSubscriptionRepository(databasePath);

			await reopenedRepository.InitializeAsync();

			FeedSubscription reopened =
					Assert.Single(await reopenedRepository.GetAllAsync());

			Assert.Equal(retrievedAt, reopened.LastRetrieved);
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
