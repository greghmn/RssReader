
namespace RssReader.Core;

public record OpmlImportEntry(
		FeedSubscription Subscription,
		OpmlImportStatus Status);
