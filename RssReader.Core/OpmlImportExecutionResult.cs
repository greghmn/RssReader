
namespace RssReader.Core;

public record OpmlImportExecutionResult(FeedSubscription Subscription, OpmlImportExecutionStatus Status, string? ErrorMessage = null, OpmlImportStatus PlannedStatus = OpmlImportStatus.New);
