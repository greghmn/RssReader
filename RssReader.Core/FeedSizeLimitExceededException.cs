using System;

namespace RssReader.Core;

public sealed class FeedSizeLimitExceededException : Exception
{
	public FeedSizeLimitExceededException()
		: base("The feed exceeds the maximum permitted size.")
	{
	}
}
