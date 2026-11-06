using RssReader.Core;

namespace RssReader.Core.Tests;

public class SubscriptionImportServiceTests
{
	[Fact]
	public async Task ImportAsync_WhenAlreadySubscribed_ReportsExistingSubscription()
	{
		var repository = new FakeSubscriptionRepository();
		var subscriptionService = new SubscriptionService(repository);
		var opmlService = new OpmlService();

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Original Title"
		});

		var importService = new SubscriptionImportService(
				opmlService,
				subscriptionService);

		const string xml = """
            <opml version="2.0">
              <body>
                <outline text="Imported Title"
                         xmlUrl="https://example.com/feed.xml" />
              </body>
            </opml>
            """;

		SubscriptionImportResult result =
				await importService.ImportAsync(xml);

		Assert.Empty(result.Added);
		Assert.Empty(result.Issues);

		FeedSubscription existing =
				Assert.Single(result.AlreadySubscribed);

		Assert.Equal("https://example.com/feed.xml", existing.Url);

		List<FeedSubscription> subscriptions =
				await subscriptionService.GetAllAsync();

		FeedSubscription stored = Assert.Single(subscriptions);

		Assert.Equal("Original Title", stored.Title);
	}

	[Fact]
	public async Task ImportAsync_WithMixedEntries_ReportsAllOutcomes()
	{
		var repository = new FakeSubscriptionRepository();
		var subscriptionService = new SubscriptionService(repository);
		var importService = new SubscriptionImportService(
				new OpmlService(),
				subscriptionService);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/existing.xml",
			Title = "Original Title"
		});

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="New Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Imported Existing Feed"
                     xmlUrl="https://example.com/existing.xml" />
            <outline text="Broken Feed"
                     xmlUrl="not-a-url" />
          </body>
        </opml>
        """;

		SubscriptionImportResult result =
				await importService.ImportAsync(xml);

		FeedSubscription added = Assert.Single(result.Added);
		Assert.Equal("New Feed", added.Title);
		Assert.Equal("https://example.com/new.xml", added.Url);

		FeedSubscription existing =
				Assert.Single(result.AlreadySubscribed);
		Assert.Equal(
				"https://example.com/existing.xml",
				existing.Url);

		OpmlImportIssue issue = Assert.Single(result.Issues);
		Assert.Equal("not-a-url", issue.Url);
		Assert.Equal("Invalid feed URL.", issue.Reason);

		List<FeedSubscription> stored =
				await subscriptionService.GetAllAsync();

		Assert.Equal(2, stored.Count);

		Assert.Contains(stored, subscription =>
				subscription.Url == "https://example.com/new.xml");

		Assert.Contains(stored, subscription =>
				subscription.Url == "https://example.com/existing.xml"
				&& subscription.Title == "Original Title");
	}

	[Fact]
	public async Task ImportAsync_WhenDatabaseFails_PreservesEarlierAdditions()
	{
		var repository = new FakeSubscriptionRepository
		{
			FailOnAddNumber = 2
		};

		var subscriptionService = new SubscriptionService(repository);
		var importService = new SubscriptionImportService(
				new OpmlService(),
				subscriptionService);

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="First Feed"
                     xmlUrl="https://example.com/first.xml" />
            <outline text="Second Feed"
                     xmlUrl="https://example.com/second.xml" />
            <outline text="Third Feed"
                     xmlUrl="https://example.com/third.xml" />
          </body>
        </opml>
        """;

		SubscriptionImportResult result =
				await importService.ImportAsync(xml);

		FeedSubscription added = Assert.Single(result.Added);
		Assert.Equal("First Feed", added.Title);

		SubscriptionImportFailure failure =
				Assert.Single(result.Failures);

		Assert.Equal("Second Feed", failure.Subscription.Title);

		FeedSubscription notAttempted =
		Assert.Single(result.NotAttempted);

		Assert.Equal("Third Feed", notAttempted.Title);
		Assert.Equal(
				"https://example.com/third.xml",
				notAttempted.Url);

		Assert.Equal(2, repository.AddCount);

		Assert.DoesNotContain(
				result.Added,
				subscription => subscription.Url ==
						"https://example.com/third.xml");

		Assert.Equal(2, repository.AddCount);

		List<FeedSubscription> stored =
				await subscriptionService.GetAllAsync();

		FeedSubscription subscription = Assert.Single(stored);

		Assert.Equal("First Feed", subscription.Title);
		Assert.Equal(
				"https://example.com/first.xml",
				subscription.Url);
	}

	[Fact]
	public async Task ImportAsync_WhenDatabaseFails_ReportsPartialSuccess()
	{
		var repository = new FakeSubscriptionRepository
		{
			FailOnAddNumber = 2
		};

		var subscriptionService = new SubscriptionService(repository);
		var importService = new SubscriptionImportService(
				new OpmlService(),
				subscriptionService);

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="First Feed"
                     xmlUrl="https://example.com/first.xml" />
            <outline text="Second Feed"
                     xmlUrl="https://example.com/second.xml" />
          </body>
        </opml>
        """;

		SubscriptionImportResult result =
				await importService.ImportAsync(xml);

		FeedSubscription added = Assert.Single(result.Added);
		Assert.Equal("First Feed", added.Title);

		SubscriptionImportFailure failure =
				Assert.Single(result.Failures);

		Assert.Equal("Second Feed", failure.Subscription.Title);
		Assert.Contains(
				"Simulated subscription database failure",
				failure.Reason);

		Assert.Empty(result.AlreadySubscribed);
		Assert.Empty(result.Issues);

		List<FeedSubscription> stored =
				await subscriptionService.GetAllAsync();

		FeedSubscription saved = Assert.Single(stored);
		Assert.Equal("First Feed", saved.Title);
	}
}