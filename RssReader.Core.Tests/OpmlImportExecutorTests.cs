
using Xunit;

namespace RssReader.Core.Tests;

public class OpmlImportExecutorTests
{
	[Fact]
	public async Task ExecuteAsync_WithNewSubscription_AddsSubscription()
	{
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example News"
		};

		var entries = new List<OpmlImportEntry>
				{
						new(subscription, OpmlImportStatus.New)
				};

		var results = await OpmlImportExecutor.ExecuteAsync(
				entries,
				service);

		var result = Assert.Single(results);

		Assert.Equal(
				OpmlImportExecutionStatus.Added,
				result.Status);

		Assert.Same(
				subscription,
				result.Subscription);

		Assert.Equal(1, repository.AddCount);

		var saved = await repository.GetAllAsync();

		var savedSubscription = Assert.Single(saved);

		Assert.Same(subscription, savedSubscription);
	}
	[Theory]
	[InlineData(OpmlImportStatus.InvalidUrl)]
	[InlineData(OpmlImportStatus.DuplicateInFile)]
	[InlineData(OpmlImportStatus.AlreadySubscribed)]
	public async Task ExecuteAsync_WithIneligibleEntry_DoesNotAddSubscription(OpmlImportStatus status)
	{
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example News"
		};

		var entries = new List<OpmlImportEntry>
		{
				new(subscription, status)
		};

		await OpmlImportExecutor.ExecuteAsync(entries, service);

		Assert.Equal(0, repository.AddCount);
		Assert.Empty(await repository.GetAllAsync());
	}

	[Theory]
	[InlineData(OpmlImportStatus.InvalidUrl)]
	[InlineData(OpmlImportStatus.DuplicateInFile)]
	[InlineData(OpmlImportStatus.AlreadySubscribed)]
	public async Task ExecuteAsync_WithIneligibleEntry_ReportsSkipped(
			OpmlImportStatus status)
	{
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example News"
		};

		var entries = new List<OpmlImportEntry>
		{
				new(subscription, status)
		};

		var results = await OpmlImportExecutor.ExecuteAsync(
				entries,
				service);

		var result = Assert.Single(results);

		Assert.Equal(
				OpmlImportExecutionStatus.Skipped,
				result.Status);

		Assert.Equal(
				status,
				result.PlannedStatus);

		Assert.Same(
				subscription,
				result.Subscription);

		Assert.Equal(0, repository.AddCount);
	}

	[Fact]
	public async Task ExecuteAsync_WhenOneAdditionFails_ContinuesImporting()
	{
		var repository = new FakeSubscriptionRepository
		{
			FailOnAddNumber = 2
		};

		var service = new SubscriptionService(repository);

		var subscriptions = new[]
		{
				new FeedSubscription
				{
						Url = "https://example.com/first",
						Title = "First Feed"
				},
				new FeedSubscription
				{
						Url = "https://example.com/second",
						Title = "Second Feed"
				},
				new FeedSubscription
				{
						Url = "https://example.com/third",
						Title = "Third Feed"
				}
		};

		var entries = subscriptions
				.Select(subscription => new OpmlImportEntry(
						subscription,
						OpmlImportStatus.New))
				.ToList();

		var results = await OpmlImportExecutor.ExecuteAsync(
				entries,
				service);

		Assert.Equal(3, results.Count);

		Assert.Equal(
				OpmlImportExecutionStatus.Added,
				results[0].Status);

		Assert.Equal(
				OpmlImportExecutionStatus.Failed,
				results[1].Status);

		Assert.Equal(
				OpmlImportExecutionStatus.Added,
				results[2].Status);

		Assert.NotNull(results[1].ErrorMessage);
		Assert.Contains(
				"Simulated subscription database failure",
				results[1].ErrorMessage);

		Assert.Equal(3, repository.AddCount);

		var saved = await repository.GetAllAsync();

		Assert.Equal(2, saved.Count);
		Assert.Contains(saved, s => s.Url == subscriptions[0].Url);
		Assert.Contains(saved, s => s.Url == subscriptions[2].Url);
		Assert.DoesNotContain(saved, s => s.Url == subscriptions[1].Url);
	}

	[Fact]
	public async Task ExecuteAsync_WhenSubscriptionAlreadyAdded_ReportsAlreadySubscribed()
	{
		var repository = new FakeSubscriptionRepository();
		var service = new SubscriptionService(repository);

		var existingSubscription = new FeedSubscription
		{
			Url = "https://example.com/feed",
			Title = "Existing Title"
		};

		await service.AddAsync(existingSubscription);

		var importedSubscription = new FeedSubscription
		{
			Url = "https://example.com/feed/",
			Title = "Imported Title"
		};

		var entries = new List<OpmlImportEntry>
		{
				new(importedSubscription, OpmlImportStatus.New)
		};

		var results = await OpmlImportExecutor.ExecuteAsync(
				entries,
				service);

		var result = Assert.Single(results);

		Assert.Equal(
				OpmlImportExecutionStatus.AlreadySubscribed,
				result.Status);

		Assert.Equal(
				OpmlImportStatus.New,
				result.PlannedStatus);

		Assert.Equal(1, repository.AddCount);

		var saved = await repository.GetAllAsync();

		var subscription = Assert.Single(saved);

		Assert.Same(existingSubscription, subscription);

		Assert.Equal("Existing Title", subscription.Title);
	}

	[Fact]
	public async Task ExecuteAsync_WithMixedEntries_ReportsAllOutcomes()
	{
		var repository = new FakeSubscriptionRepository
		{
			FailOnAddNumber = 2
		};

		var service = new SubscriptionService(repository);

		var entries = new List<OpmlImportEntry>
		{
				new(
						new FeedSubscription
						{
								Url = "https://example.com/first",
								Title = "First Feed"
						},
						OpmlImportStatus.New),

				new(
						new FeedSubscription
						{
								Url = "not-a-valid-url",
								Title = "Invalid Feed"
						},
						OpmlImportStatus.InvalidUrl),

				new(
						new FeedSubscription
						{
								Url = "https://example.com/second",
								Title = "Second Feed"
						},
						OpmlImportStatus.New),

				new(
						new FeedSubscription
						{
								Url = "https://example.com/first/",
								Title = "Duplicate Feed"
						},
						OpmlImportStatus.DuplicateInFile),

				new(
						new FeedSubscription
						{
								Url = "https://example.com/third",
								Title = "Third Feed"
						},
						OpmlImportStatus.New)
		};

		var results = await OpmlImportExecutor.ExecuteAsync(
				entries,
				service);

		Assert.Equal(5, results.Count);

		Assert.Equal(
				OpmlImportExecutionStatus.Added,
				results[0].Status);

		Assert.Equal(
				OpmlImportExecutionStatus.Skipped,
				results[1].Status);

		Assert.Equal(
				OpmlImportExecutionStatus.Failed,
				results[2].Status);

		Assert.Equal(
				OpmlImportExecutionStatus.Skipped,
				results[3].Status);

		Assert.Equal(
				OpmlImportExecutionStatus.Added,
				results[4].Status);

		Assert.Equal(
				OpmlImportStatus.InvalidUrl,
				results[1].PlannedStatus);

		Assert.Equal(
				OpmlImportStatus.DuplicateInFile,
				results[3].PlannedStatus);

		Assert.NotNull(results[2].ErrorMessage);

		Assert.Equal(3, repository.AddCount);

		var saved = await repository.GetAllAsync();

		Assert.Equal(2, saved.Count);
		Assert.Contains(saved, s => s.Url == "https://example.com/first");
		Assert.Contains(saved, s => s.Url == "https://example.com/third");
	}

	[Fact]
	public async Task ExecuteAsync_WhenOperationIsCanceled_PropagatesCancellation()
	{
		var repository = new FakeSubscriptionRepository
		{
			CancelOnAdd = true
		};

		var service = new SubscriptionService(repository);

		var entries = new List<OpmlImportEntry>
		{
				new(
						new FeedSubscription
						{
								Url = "https://example.com/feed",
								Title = "Example Feed"
						},
						OpmlImportStatus.New)
		};

		await Assert.ThrowsAsync<OperationCanceledException>(
				() => OpmlImportExecutor.ExecuteAsync(
						entries,
						service));
	}

}
