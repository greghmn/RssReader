
namespace RssReader.Core;

public static class OpmlImportExecutor
{
	public static async Task<List<OpmlImportExecutionResult>> ExecuteAsync(
			IEnumerable<OpmlImportEntry> entries,
			SubscriptionService service)
	{
		var results = new List<OpmlImportExecutionResult>();

		foreach (OpmlImportEntry entry in entries)
		{


			if (entry.Status != OpmlImportStatus.New)
			{
				results.Add(new OpmlImportExecutionResult(
						entry.Subscription,
						OpmlImportExecutionStatus.Skipped,
						PlannedStatus: entry.Status));

				continue;
			}



			try
			{
				bool added = await service.AddAsync(entry.Subscription);

				results.Add(new OpmlImportExecutionResult(
						entry.Subscription,
						added
								? OpmlImportExecutionStatus.Added
								: OpmlImportExecutionStatus.AlreadySubscribed));
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				results.Add(new OpmlImportExecutionResult(
						entry.Subscription,
						OpmlImportExecutionStatus.Failed,
						ErrorMessage: ex.Message));
			}

		}

		return results;
	}
}
