namespace RssReader.Core;

public class FeedSubscription
{
	public string Url { get; init; } = "";

	public string Title { get; set; } = "";

	public DateTimeOffset DateAdded { get; init; } =
			DateTimeOffset.UtcNow;
	public DateTimeOffset? LastRetrieved { get; set; }

}
