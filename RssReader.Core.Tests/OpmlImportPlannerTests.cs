
using Xunit;

namespace RssReader.Core.Tests;

public class OpmlImportPlannerTests
{
	[Fact]
	public void Plan_WithNewSubscription_ClassifiesAsNew()
	{
		var candidates = new List<FeedSubscription>
				{
						new()
						{
								Url = "https://example.com/feed.xml",
								Title = "Example News"
						}
				};

		var existing = new List<FeedSubscription>();

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		var result = Assert.Single(results);

		Assert.Equal(
				OpmlImportStatus.New,
				result.Status);

		Assert.Equal(
				"https://example.com/feed.xml",
				result.Subscription.Url);

		Assert.Equal(
				"Example News",
				result.Subscription.Title);
	}

	[Fact]
	public void Plan_WithInvalidUrl_ClassifiesAsInvalidUrl()
	{
		var candidates = new List<FeedSubscription>
		{
				new()
				{
						Url = "not-a-valid-url",
						Title = "Broken Feed"
				}
		};

		var existing = new List<FeedSubscription>();

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		var result = Assert.Single(results);

		Assert.Equal(
				OpmlImportStatus.InvalidUrl,
				result.Status);

		Assert.Equal(
				"not-a-valid-url",
				result.Subscription.Url);

		Assert.Equal(
				"Broken Feed",
				result.Subscription.Title);
	}

	[Fact]
	public void Plan_WithExistingSubscription_ClassifiesAsAlreadySubscribed()
	{
		var candidates = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/feed.xml/",
						Title = "Imported Title"
				}
		};

		var existing = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/feed.xml",
						Title = "Existing Title"
				}
		};

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		var result = Assert.Single(results);

		Assert.Equal(
				OpmlImportStatus.AlreadySubscribed,
				result.Status);

		Assert.Equal(
				"Imported Title",
				result.Subscription.Title);
	}

	[Fact]
	public void Plan_WithDuplicateCandidates_ClassifiesSecondAsDuplicateInFile()
	{
		var candidates = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/feed",
						Title = "Original Title"
				},
				new()
				{
						Url = "https://example.com/feed/",
						Title = "Duplicate Title"
				}
		};

		var existing = new List<FeedSubscription>();

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		Assert.Equal(2, results.Count);

		Assert.Equal(
				OpmlImportStatus.New,
				results[0].Status);

		Assert.Equal(
				OpmlImportStatus.DuplicateInFile,
				results[1].Status);

		Assert.Equal(
				"Duplicate Title",
				results[1].Subscription.Title);
	}

	[Fact]
	public void Plan_WithRepeatedExistingSubscription_ClassifiesBothAsAlreadySubscribed()
	{
		var candidates = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/feed",
						Title = "First Title"
				},
				new()
				{
						Url = "https://example.com/feed/",
						Title = "Second Title"
				}
		};

		var existing = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/feed",
						Title = "Existing Title"
				}
		};

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		Assert.Equal(2, results.Count);

		Assert.All(results, result =>
				Assert.Equal(
						OpmlImportStatus.AlreadySubscribed,
						result.Status));

		Assert.Equal(
				"First Title",
				results[0].Subscription.Title);

		Assert.Equal(
				"Second Title",
				results[1].Subscription.Title);
	}

	[Fact]
	public void Plan_WithMixedCandidates_ClassifiesEachCorrectly()
	{
		var candidates = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/new",
						Title = "New Feed"
				},
				new()
				{
						Url = "not-a-valid-url",
						Title = "Invalid Feed"
				},
				new()
				{
						Url = "https://example.com/existing",
						Title = "Existing Feed"
				},
				new()
				{
						Url = "https://example.com/new/",
						Title = "Duplicate Feed"
				},
				new()
				{
						Url = "https://example.com/another",
						Title = "Another New Feed"
				}
		};

		var existing = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/existing",
						Title = "Previously Subscribed"
				}
		};

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		Assert.Equal(5, results.Count);

		Assert.Equal(
				OpmlImportStatus.New,
				results[0].Status);

		Assert.Equal(
				OpmlImportStatus.InvalidUrl,
				results[1].Status);

		Assert.Equal(
				OpmlImportStatus.AlreadySubscribed,
				results[2].Status);

		Assert.Equal(
				OpmlImportStatus.DuplicateInFile,
				results[3].Status);

		Assert.Equal(
				OpmlImportStatus.New,
				results[4].Status);

		for (int i = 0; i < candidates.Count; i++)
		{
			Assert.Same(
					candidates[i],
					results[i].Subscription);
		}
	}

	[Fact]
	public void Plan_WithNoCandidates_ReturnsEmptyList()
	{
		var candidates = new List<FeedSubscription>();

		var existing = new List<FeedSubscription>
		{
				new()
				{
						Url = "https://example.com/existing",
						Title = "Existing Feed"
				}
		};

		var results = OpmlImportPlanner.Plan(
				candidates,
				existing);

		Assert.Empty(results);
	}

}
