namespace RssReader.Core;

public class SubscriptionImportFailure
{
	public FeedSubscription Subscription { get; init; } = null!;

	public string Reason { get; init; } = "";
}