// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
namespace RssReader.Core;

public class FeedSubscription
{
	public string Url { get; init; } = "";

	public string Title { get; set; } = "";

	public DateTimeOffset DateAdded { get; init; } =
			DateTimeOffset.UtcNow;
	public DateTimeOffset? LastRetrieved { get; set; }

}
