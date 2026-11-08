// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using Microsoft.Data.Sqlite;

namespace RssReader.Core;

public class SqliteSubscriptionRepository : ISubscriptionRepository
{
	private readonly string _connectionString;

	public SqliteSubscriptionRepository(string databasePath)
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
    CREATE TABLE IF NOT EXISTS Subscriptions (
        Url TEXT NOT NULL PRIMARY KEY,
        Title TEXT NOT NULL,
        DateAdded TEXT NOT NULL,
        LastRetrieved TEXT NULL
    );
    """;

		await command.ExecuteNonQueryAsync();

		// Check whether LastRetrieved already exists.
		await using var checkCommand = connection.CreateCommand();

		checkCommand.CommandText = """
    PRAGMA table_info(Subscriptions);
    """;

		bool hasLastRetrieved = false;

		await using (var reader = await checkCommand.ExecuteReaderAsync())
		{
			while (await reader.ReadAsync())
			{
				if (reader.GetString(1) == "LastRetrieved")
				{
					hasLastRetrieved = true;
					break;
				}
			}
		}

		// Add the column to databases created by older versions.
		if (!hasLastRetrieved)
		{
			await using var alterCommand = connection.CreateCommand();

			alterCommand.CommandText = """
        ALTER TABLE Subscriptions
        ADD COLUMN LastRetrieved TEXT NULL;
        """;

			await alterCommand.ExecuteNonQueryAsync();
		}
	}

	public async Task<List<FeedSubscription>> GetAllAsync()
	{
		var subscriptions = new List<FeedSubscription>();

		await using var connection =
				new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
            SELECT Url, Title, DateAdded, LastRetrieved
            FROM Subscriptions
            ORDER BY DateAdded;
            """;

		await using var reader = await command.ExecuteReaderAsync();

		while (await reader.ReadAsync())
		{
			subscriptions.Add(new FeedSubscription
			{
				Url = reader.GetString(0),
				Title = reader.GetString(1),
				DateAdded = DateTimeOffset.Parse(reader.GetString(2)),
				LastRetrieved = reader.IsDBNull(3)
							? null
							: DateTimeOffset.Parse(reader.GetString(3))
			});
		}

		return subscriptions;
	}

	public async Task AddAsync(FeedSubscription subscription)
	{
		await using var connection =
				new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
            INSERT INTO Subscriptions (Url, Title, DateAdded)
            VALUES ($url, $title, $dateAdded);
            """;

		command.Parameters.AddWithValue("$url", subscription.Url);
		command.Parameters.AddWithValue("$title", subscription.Title);
		command.Parameters.AddWithValue(
				"$dateAdded",
				subscription.DateAdded.ToString("O"));

		await command.ExecuteNonQueryAsync();
	}

	public async Task<bool> RemoveAsync(string url)
	{
		await using var connection =
				new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
            DELETE FROM Subscriptions
            WHERE Url = $url;
            """;

		command.Parameters.AddWithValue("$url", url);

		int rowsAffected = await command.ExecuteNonQueryAsync();

		return rowsAffected > 0;
	}

	public async Task<bool> UpdateTitleAsync(string url, string title)
	{
		await using var connection =
				new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
        UPDATE Subscriptions
        SET Title = $title
        WHERE Url = $url;
        """;

		command.Parameters.AddWithValue("$url", url);
		command.Parameters.AddWithValue("$title", title);

		int rowsAffected = await command.ExecuteNonQueryAsync();

		return rowsAffected > 0;
	}

	public async Task<bool> UpdateLastRetrievedAsync(
		string url,
		DateTimeOffset lastRetrieved)
	{
		await using var connection =
				new SqliteConnection(_connectionString);

		await connection.OpenAsync();

		await using var command = connection.CreateCommand();

		command.CommandText = """
        UPDATE Subscriptions
        SET LastRetrieved = $lastRetrieved
        WHERE Url = $url;
        """;

		command.Parameters.AddWithValue("$url", url);
		command.Parameters.AddWithValue(
				"$lastRetrieved",
				lastRetrieved.ToString("O"));

		int rowsAffected = await command.ExecuteNonQueryAsync();

		return rowsAffected > 0;
	}

}
