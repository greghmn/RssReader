using Microsoft.Data.Sqlite;
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

	[Fact]
	public async Task ExportThenImport_PersistsSubscriptionsInNewRepository()
	{
		var opmlService = new OpmlService();

		var sourceRepository = new FakeSubscriptionRepository();
		var sourceService = new SubscriptionService(sourceRepository);

		await sourceService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/first.xml",
			Title = "First Feed"
		});

		await sourceService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/second.xml",
			Title = "Science & Technology"
		});

		List<FeedSubscription> sourceSubscriptions =
				await sourceService.GetAllAsync();

		string xml = opmlService.Export(sourceSubscriptions);

		var destinationRepository = new FakeSubscriptionRepository();
		var destinationService =
				new SubscriptionService(destinationRepository);

		var importService = new SubscriptionImportService(
				opmlService,
				destinationService);

		SubscriptionImportResult result =
				await importService.ImportAsync(xml);

		Assert.Equal(2, result.Added.Count);
		Assert.Empty(result.AlreadySubscribed);
		Assert.Empty(result.Issues);
		Assert.Empty(result.Failures);
		Assert.Empty(result.NotAttempted);

		List<FeedSubscription> imported =
				await destinationService.GetAllAsync();

		Assert.Equal(2, imported.Count);

		Assert.Equal(sourceSubscriptions[0].Url, imported[0].Url);
		Assert.Equal(sourceSubscriptions[0].Title, imported[0].Title);

		Assert.Equal(sourceSubscriptions[1].Url, imported[1].Url);
		Assert.Equal(sourceSubscriptions[1].Title, imported[1].Title);
	}

	[Fact]
	public async Task ExportThenImport_PersistsSubscriptionsAcrossSqliteDatabases()
	{
		string sourcePath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		string destinationPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid()}.db");

		try
		{
			var sourceRepository =
					new SqliteSubscriptionRepository(sourcePath);

			var destinationRepository =
					new SqliteSubscriptionRepository(destinationPath);

			await sourceRepository.InitializeAsync();
			await destinationRepository.InitializeAsync();

			var sourceService =
					new SubscriptionService(sourceRepository);

			var destinationService =
					new SubscriptionService(destinationRepository);

			await sourceService.AddAsync(new FeedSubscription
			{
				Url = "https://example.com/first.xml",
				Title = "First Feed"
			});

			await sourceService.AddAsync(new FeedSubscription
			{
				Url = "https://example.com/second.xml",
				Title = "Science & Technology"
			});

			var opmlService = new OpmlService();

			List<FeedSubscription> original =
					await sourceService.GetAllAsync();

			string xml = opmlService.Export(original);

			var importService = new SubscriptionImportService(
					opmlService,
					destinationService);

			SubscriptionImportResult result =
					await importService.ImportAsync(xml);

			Assert.Equal(2, result.Added.Count);
			Assert.Empty(result.Failures);
			Assert.Empty(result.NotAttempted);
			Assert.Empty(result.Issues);

			var reopenedRepository =
					new SqliteSubscriptionRepository(destinationPath);

			await reopenedRepository.InitializeAsync();

			var reopenedService =
					new SubscriptionService(reopenedRepository);

			List<FeedSubscription> imported =
					await reopenedService.GetAllAsync();

			Assert.Equal(2, imported.Count);

			foreach (FeedSubscription subscription in original)
			{
				FeedSubscription match = Assert.Single(
						imported,
						item => item.Url == subscription.Url);

				Assert.Equal(subscription.Title, match.Title);
			}
		}
		finally
		{
			SqliteConnection.ClearAllPools();

			if (File.Exists(sourcePath))
			{
				File.Delete(sourcePath);
			}

			if (File.Exists(destinationPath))
			{
				File.Delete(destinationPath);
			}
		}
	}
}