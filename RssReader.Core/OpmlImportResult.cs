namespace RssReader.Core;

public class OpmlImportResult
{
	//parser recognizes these as usable
	public List<FeedSubscription> Subscriptions { get; } = new();

	//...and these require attention
	public List<OpmlImportIssue> Issues { get; } = new();
}

public class OpmlImportIssue
{
	public string? Url { get; init; }

	public string? Title { get; init; }

	public string Reason { get; init; } = "";
}