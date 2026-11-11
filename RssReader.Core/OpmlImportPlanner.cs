
namespace RssReader.Core;

public static class OpmlImportPlanner
{
	public static List<OpmlImportEntry> Plan(IEnumerable<FeedSubscription> candidates, IEnumerable<FeedSubscription> existing)
	{
		var results = new List<OpmlImportEntry>();
		var seenUrls = new List<string>();


		foreach (FeedSubscription candidate in candidates)
		{
			OpmlImportStatus status;

			if (!SubscriptionUrl.IsValid(candidate.Url))
			{
				status = OpmlImportStatus.InvalidUrl;
			}
			else if (existing.Any(subscription =>
							SubscriptionUrl.Matches(
									subscription.Url,
									candidate.Url)))
			{
				status = OpmlImportStatus.AlreadySubscribed;
			}

			else if (seenUrls.Any(url =>
							SubscriptionUrl.Matches(url, candidate.Url)))
			{
				status = OpmlImportStatus.DuplicateInFile;
			}

			else
			{
				status = OpmlImportStatus.New;
			}

			results.Add(new OpmlImportEntry(
					candidate,
					status));
			if (SubscriptionUrl.IsValid(candidate.Url))
			{
				seenUrls.Add(candidate.Url);
			}
		}



		return results;
	}
}
