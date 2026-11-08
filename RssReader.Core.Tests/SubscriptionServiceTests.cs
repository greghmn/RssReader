// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using RssReader.Core;

namespace RssReader.Core.Tests;

public class SubscriptionServiceTests
{
	[Fact]
	public async Task AddAsync_ValidSubscription_AddsIt()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		};

		// Act
		bool result = await service.AddAsync(subscription);
		List<FeedSubscription> subscriptions =
				await service.GetAllAsync();

		// Assert
		Assert.True(result);
		Assert.Single(subscriptions);
		Assert.Equal(subscription.Url, subscriptions[0].Url);
	}

	//Testing invalid subscription URLs 

	[Theory]
	[InlineData("google.com")]
	[InlineData("ftp://example.com/feed.xml")]
	[InlineData("file:///feed.xml")]
	public async Task AddAsync_InvalidUrl_ThrowsArgumentException(
			string url)
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var subscription = new FeedSubscription
		{
			Url = url,
			Title = "Invalid Feed"
		};

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(
				() => service.AddAsync(subscription));

		Assert.Empty(await service.GetAllAsync());
	}

	[Fact]
	public async Task AddAsync_DuplicateUrl_ReturnsFalse()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		await service.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		});

		// Act
		bool result = await service.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Duplicate Feed"
		});

		List<FeedSubscription> subscriptions =
				await service.GetAllAsync();

		// Assert
		Assert.False(result);
		Assert.Single(subscriptions);
	}

	[Fact]
	public async Task RemoveAsync_ExistingSubscription_RemovesIt()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		await service.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		});

		// Act
		bool result = await service.RemoveAsync(
				"https://example.com/feed.xml");

		List<FeedSubscription> subscriptions =
				await service.GetAllAsync();

		// Assert
		Assert.True(result);
		Assert.Empty(subscriptions);
	}

	[Fact]
	public async Task RemoveAsync_UnknownUrl_ReturnsFalse()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		// Act
		bool result = await service.RemoveAsync(
				"https://example.com/missing.xml");

		// Assert
		Assert.False(result);
		Assert.Empty(await service.GetAllAsync());
	}

	[Fact]
	public async Task RenameAsync_ExistingSubscription_UpdatesTitle()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var dateAdded = new DateTimeOffset(
				2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Original Title",
			DateAdded = dateAdded
		};

		await service.AddAsync(subscription);

		// Act
		bool result = await service.RenameAsync(
				subscription.Url,
				"New Title");

		List<FeedSubscription> subscriptions =
				await service.GetAllAsync();

		// Assert
		Assert.True(result);
		Assert.Single(subscriptions);
		Assert.Equal("New Title", subscriptions[0].Title);
		Assert.Equal(subscription.Url, subscriptions[0].Url);
		Assert.Equal(dateAdded, subscriptions[0].DateAdded);
	}

	[Theory]
	[InlineData(
		"https://example.com/feed.xml",
		"https://EXAMPLE.COM/feed.xml")]
	[InlineData(
		"https://example.com/feed.xml",
		"https://example.com/feed.xml/")]
	[InlineData(
		"https://example.com/feed.xml",
		"https://EXAMPLE.COM/feed.xml/")]
	public async Task RenameAsync_VariationsOfUrl_UpdatesTitle(
		string storedUrl,
		string suppliedUrl)
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		await service.AddAsync(new FeedSubscription
		{
			Url = storedUrl,
			Title = "Original Title"
		});

		// Act
		bool result = await service.RenameAsync(
				suppliedUrl,
				"New Title");

		List<FeedSubscription> subscriptions =
				await service.GetAllAsync();

		// Assert
		Assert.True(result);
		Assert.Single(subscriptions);
		Assert.Equal("New Title", subscriptions[0].Title);
		Assert.Equal(storedUrl, subscriptions[0].Url);
	}

	[Fact]
	public async Task RenameAsync_UnknownUrl_ReturnsFalse()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		// Act
		bool result = await service.RenameAsync(
				"https://example.com/missing.xml",
				"New Title");

		// Assert
		Assert.False(result);
		Assert.Empty(await service.GetAllAsync());
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	public async Task RenameAsync_EmptyTitle_ThrowsArgumentException(
		string title)
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		await service.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Original Title"
		});

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(
				() => service.RenameAsync(
						"https://example.com/feed.xml",
						title));

		List<FeedSubscription> subscriptions =
				await service.GetAllAsync();

		Assert.Single(subscriptions);
		Assert.Equal("Original Title", subscriptions[0].Title);
	}
}
