namespace RssReader.Core;

public class SubscriptionImportService
{
	private readonly OpmlService _opmlService;
	private readonly SubscriptionService _subscriptionService;

	public SubscriptionImportService(
			OpmlService opmlService,
			SubscriptionService subscriptionService)
	{
		_opmlService = opmlService;
		_subscriptionService = subscriptionService;
	}

	public async Task<SubscriptionImportResult> ImportAsync(string xml)
	{
		OpmlImportResult parsed = _opmlService.ImportWithIssues(xml);

		var result = new SubscriptionImportResult();

		result.Issues.AddRange(parsed.Issues);

		for (int i = 0; i < parsed.Subscriptions.Count; i++)
		{
			FeedSubscription subscription = parsed.Subscriptions[i];

			try
			{
				bool added =
						await _subscriptionService.AddAsync(subscription);

				if (added)
				{
					result.Added.Add(subscription);
				}
				else
				{
					result.AlreadySubscribed.Add(subscription);
				}
			}
			catch (Exception ex)
			{
				result.Failures.Add(new SubscriptionImportFailure
				{
					Subscription = subscription,
					Reason = ex.Message
				});

				result.NotAttempted.AddRange(
						parsed.Subscriptions.Skip(i + 1));

				break;
			}
		}

		return result;
	}
}