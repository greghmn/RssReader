namespace RssReader.Core;

public static class SubscriptionUrl
{
	public static bool Matches(string first, string second)
	{
		return string.Equals(
						first.TrimEnd('/'),
						second.TrimEnd('/'),
						StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsValid(string url)
	{
		return Uri.TryCreate(
						url,
						UriKind.Absolute,
						out Uri? feedUri)
				&& (feedUri.Scheme == Uri.UriSchemeHttp
						|| feedUri.Scheme == Uri.UriSchemeHttps);
	}

}
