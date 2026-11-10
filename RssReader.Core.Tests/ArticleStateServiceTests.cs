// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using Xunit;

namespace RssReader.Core.Tests;

public class ArticleStateServiceTests
{

	//yes, there are penalties in the Pro Bowl

	[Fact]
	public async Task IsReadAsync_WhenItemHasIdentity_UsesRepository()
	{
		var repository = new FakeArticleStateRepository
		{
			IsReadResult = true
		};
		var service = new ArticleStateService(repository);
		var item = new FeedItem { Id = "article-123" };

		bool result = await service.IsReadAsync(
				"https://example.com/feed", item);

		Assert.True(result);
		Assert.Equal("article-123", repository.LastArticleId);
	}

	[Fact]
	public async Task IsReadAsync_WhenItemHasNoIdentity_ReturnsFalse()
	{
		var repository = new FakeArticleStateRepository();
		var service = new ArticleStateService(repository);
		var item = new FeedItem();

		bool result = await service.IsReadAsync(
				"https://example.com/feed", item);

		Assert.False(result);
		Assert.False(repository.WasCalled);
	}

	[Fact]
	public async Task MarkAsReadAsync_WhenItemHasNoIdentity_SkipsRepository()
	{
		var repository = new FakeArticleStateRepository();
		var service = new ArticleStateService(repository);

		await service.MarkAsReadAsync(
				"https://example.com/feed", new FeedItem());

		Assert.False(repository.WasCalled);
	}

	[Fact]
	public async Task MarkAsUnreadAsync_WhenItemHasNoIdentity_SkipsRepository()
	{
		var repository = new FakeArticleStateRepository();
		var service = new ArticleStateService(repository);

		await service.MarkAsUnreadAsync(
				"https://example.com/feed", new FeedItem());

		Assert.False(repository.WasCalled);
	}

	private sealed class FakeArticleStateRepository
			: IArticleStateRepository
	{
		public bool IsReadResult { get; set; }
		public bool WasCalled { get; private set; }
		public string? LastArticleId { get; private set; }

		public Task<bool> IsReadAsync(
				string feedUrl, string articleId)
		{
			WasCalled = true;
			LastArticleId = articleId;
			return Task.FromResult(IsReadResult);
		}

		public Task MarkAsReadAsync(
				string feedUrl, string articleId)
		{
			WasCalled = true;
			LastArticleId = articleId;
			return Task.CompletedTask;
		}

		public Task MarkAsUnreadAsync(
				string feedUrl, string articleId)
		{
			WasCalled = true;
			LastArticleId = articleId;
			return Task.CompletedTask;
		}
	}

	//but wait there's more 


	[Fact]
	public async Task MarkAsReadAsync_WhenItemHasIdentity_UsesRepository()
	{
		var repository = new FakeArticleStateRepository();
		var service = new ArticleStateService(repository);
		var item = new FeedItem { Id = "article-123" };

		await service.MarkAsReadAsync(
				"https://example.com/feed", item);

		Assert.True(repository.WasCalled);
		Assert.Equal("article-123", repository.LastArticleId);
	}

	[Fact]
	public async Task MarkAsUnreadAsync_WhenItemHasIdentity_UsesRepository()
	{
		var repository = new FakeArticleStateRepository();
		var service = new ArticleStateService(repository);
		var item = new FeedItem { Link = "https://example.com/article" };

		await service.MarkAsUnreadAsync(
				"https://example.com/feed", item);

		Assert.True(repository.WasCalled);
		Assert.Equal(
				"https://example.com/article",
				repository.LastArticleId);
	}


	[Fact]
	public async Task IsReadAsync_WhenArticleAcquiresId_UsesNewIdentity()
	{
		const string feedUrl = "https://example.com/feed";
		const string articleUrl = "https://example.com/article";

		var repository = new FakeArticleStateRepository
		{
			IsReadResult = true
		};

		var service = new ArticleStateService(repository);

		var original = new FeedItem
		{
			Link = articleUrl
		};

		var updated = new FeedItem
		{
			Id = "article-123",
			Link = articleUrl
		};

		await service.IsReadAsync(feedUrl, original);

		Assert.Equal(articleUrl, repository.LastArticleId);

		await service.IsReadAsync(feedUrl, updated);

		Assert.Equal("article-123", repository.LastArticleId);
	}

}
