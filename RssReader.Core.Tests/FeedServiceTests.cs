
using RssReader.Core;

namespace RssReader.Core.Tests;

public class FeedServiceTests
{
	private sealed class RecordingFeedReader : IFeedReader
	{
		public CancellationToken ReceivedToken { get; private set; }

		public Task<List<FeedItem>> ReadAsync(
				string feedUrl,
				CancellationToken cancellationToken = default)
		{
			ReceivedToken = cancellationToken;

			return Task.FromResult(new List<FeedItem>());
		}
	}

	[Fact]
	public async Task GetItemsAsync_UnsubscribedUrl_ThrowsException()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var subscriptionService = new SubscriptionService(repository);
		var fakeReader = new FakeFeedReader();

		var articleRepository = new InMemoryArticleRepository();
		var feedService = new FeedService(
						subscriptionService,
						fakeReader, articleRepository);

		// Act and Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
				() => feedService.GetItemsAsync(
						"https://example.com/feed.xml"));
	}

	[Fact]
	public async Task GetItemsAsync_SubscribedUrl_ReturnsFeedItems()
	{
		// Arrange
		var repository = new FakeSubscriptionRepository();
		var subscriptionService = new SubscriptionService(repository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		});

		var expectedItems = new List<FeedItem>
				{
						new()
						{
								Title = "First Article",
								Link = "https://example.com/article-1",
								Summary = "The first article."
						},
						new()
						{
								Title = "Second Article",
								Link = "https://example.com/article-2",
								Summary = "The second article."
						}
				};

		var fakeReader = new FakeFeedReader
		{
			ItemsToReturn = expectedItems
		};

		var articleRepository = new InMemoryArticleRepository();

		var feedService = new FeedService(
						subscriptionService,
						fakeReader, articleRepository);

		// Act
		List<FeedItem> actualItems =
				await feedService.GetItemsAsync(
						"https://example.com/feed.xml");

		// Assert
		Assert.Equal(expectedItems, actualItems);
		Assert.Equal(
				"https://example.com/feed.xml",
				fakeReader.LastRequestedUrl);

		List<FeedItem> storedArticles =
		await articleRepository.GetByFeedAsync(
				"https://example.com/feed.xml");

		Assert.Equal(2, storedArticles.Count);
		Assert.Contains(storedArticles, article =>
				article.Link == "https://example.com/article-1"
				&& article.Title == "First Article");

		Assert.Contains(storedArticles, article =>
				article.Link == "https://example.com/article-2"
				&& article.Title == "Second Article");

		Assert.All(storedArticles, article =>
				Assert.Equal(
						"https://example.com/feed.xml",
						article.FeedUrl));
	}

	//test retrieving stored articles after unsubscribing

	[Fact]
	public async Task GetStoredItemsAsync_AfterUnsubscribing_ReturnsSavedArticles()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();
		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Example Feed"
		});

		var article = new FeedItem
		{
			Id = "article-123",
			Link = "https://example.com/article-123",
			Title = "An article worth preserving"
		};

		var fakeReader = new FakeFeedReader
		{
			ItemsToReturn = new List<FeedItem> { article }
		};

		var articleRepository = new InMemoryArticleRepository();

		var feedService = new FeedService(
				subscriptionService,
				fakeReader,
				articleRepository);

		// Retrieve and store the article.
		await feedService.GetItemsAsync(feedUrl);

		Assert.Equal(1, fakeReader.ReadCount);

		// Remove the subscription.
		await subscriptionService.RemoveAsync(feedUrl);

		// Retrieve the article from local storage.
		List<FeedItem> storedArticles =
				await feedService.GetStoredItemsAsync(feedUrl);

		Assert.Equal(1, fakeReader.ReadCount);

		Assert.Single(storedArticles);
		Assert.Equal("article-123", storedArticles[0].Id);
		Assert.Equal("An article worth preserving", storedArticles[0].Title);
		Assert.Equal(feedUrl, storedArticles[0].FeedUrl);

		// Only the original retrieval should have contacted the reader.
		Assert.Equal(feedUrl, fakeReader.LastRequestedUrl);
	}

	[Fact]
	public async Task GetStoredItemsAsync_AfterLaterRetrieval_PreservesOlderArticles()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();
		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Example Feed"
		});

		var olderArticle = new FeedItem
		{
			Id = "older-article",
			Title = "An Older Article",
			Link = "https://example.com/older"
		};

		var newerArticle = new FeedItem
		{
			Id = "newer-article",
			Title = "A Newer Article",
			Link = "https://example.com/newer"
		};

		var fakeReader = new FakeFeedReader
		{
			ItemsToReturn = new List<FeedItem> { olderArticle }
		};

		var articleRepository = new InMemoryArticleRepository();

		var feedService = new FeedService(
				subscriptionService,
				fakeReader,
				articleRepository);

		// First retrieval contains the older article.
		await feedService.GetItemsAsync(feedUrl);

		// The publisher's next feed no longer includes it.
		fakeReader.ItemsToReturn = new List<FeedItem> { newerArticle };

		await feedService.GetItemsAsync(feedUrl);

		// Both articles should still be available locally.
		List<FeedItem> storedArticles =
				await feedService.GetStoredItemsAsync(feedUrl);

		Assert.Equal(2, storedArticles.Count);

		Assert.Contains(storedArticles,
				article => article.Id == "older-article");

		Assert.Contains(storedArticles,
				article => article.Id == "newer-article");
	}

	[Fact]
	public async Task GetItemsAsync_ForwardsCancellationTokenToFeedReader()
	{
		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		const string feedUrl = "https://example.com/feed.xml";

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var feedReader = new RecordingFeedReader();
		var articleRepository = new FakeArticleRepository();

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				articleRepository);

		using var cancellationSource =
				new CancellationTokenSource();

		await feedService.GetItemsAsync(
				feedUrl,
				cancellationSource.Token);

		Assert.Equal(
				cancellationSource.Token,
				feedReader.ReceivedToken);
	}
}
