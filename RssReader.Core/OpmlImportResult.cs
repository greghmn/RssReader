// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
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