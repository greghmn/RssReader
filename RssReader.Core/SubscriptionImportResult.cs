namespace RssReader.Core;

public class SubscriptionImportResult
{
	public List<FeedSubscription> Added { get; } = new();

	public List<FeedSubscription> AlreadySubscribed { get; } = new();

	public List<OpmlImportIssue> Issues { get; } = new();

	public List<SubscriptionImportFailure> Failures { get; } = new();

	public List<FeedSubscription> NotAttempted { get; } = new();
}