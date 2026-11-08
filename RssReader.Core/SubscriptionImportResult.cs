// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
namespace RssReader.Core;

public class SubscriptionImportResult
{
	public List<FeedSubscription> Added { get; } = new();

	public List<FeedSubscription> AlreadySubscribed { get; } = new();

	public List<OpmlImportIssue> Issues { get; } = new();

	public List<SubscriptionImportFailure> Failures { get; } = new();

	public List<FeedSubscription> NotAttempted { get; } = new();
}