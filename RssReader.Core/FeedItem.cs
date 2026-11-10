// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using System;
using System.Collections.Generic;
using System.Text;

namespace RssReader.Core
{
	public class FeedItem
	{
		public string Title { get; init; } = "";
		public string Link { get; init; } = "";
		public string Summary { get; init; } = "";
		public string Content { get; init; } = "";
		public DateTimeOffset? PublishedAt { get; init; }
		//for identifying articles 
		public string Id { get; init; } = "";
		public string FeedUrl { get; init; } = "";
		public string? RawPublishedDate { get; init; }
	}
}
