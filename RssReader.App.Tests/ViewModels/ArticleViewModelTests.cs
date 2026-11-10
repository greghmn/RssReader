// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using RssReader.App.ViewModels;
using RssReader.Core;
using Xunit;

namespace RssReader.App.Tests;

public class ArticleViewModelTests
{
	[Fact]
	public void PublishedDateText_WhenParsedDateMissing_UsesRawDate()
	{
		var item = new FeedItem
		{
			RawPublishedDate = "An unusual publication date"
		};

		var viewModel = new ArticleViewModel(item, isRead: false);

		Assert.Equal(
						"An unusual publication date",
						viewModel.PublishedDateText);
	}

	[Fact]
	public void PublishedDateText_WhenParsedDateExists_PrefersParsedDate()
	{
		var publishedAt = new DateTimeOffset(
						2026, 11, 9, 18, 2, 7,
						TimeSpan.FromHours(-5));

		var item = new FeedItem
		{
			PublishedAt = publishedAt,
			RawPublishedDate = "Original unformatted date"
		};

		var viewModel = new ArticleViewModel(item, isRead: false);

		Assert.Equal(
						publishedAt.ToLocalTime().ToString("g"),
						viewModel.PublishedDateText);
	}

	[Fact]
	public void PublishedDateText_WhenBothDatesMissing_ShowsUnknown()
	{
		var item = new FeedItem
		{
			PublishedAt = null,
			RawPublishedDate = null
		};

		var viewModel = new ArticleViewModel(item, isRead: false);

		Assert.Equal(
						"Publication date unknown",
						viewModel.PublishedDateText);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	[InlineData("\t")]
	public void PublishedDateText_WhenRawDateIsWhitespace_ShowsUnknown(
				string rawDate)
	{
		var item = new FeedItem
		{
			PublishedAt = null,
			RawPublishedDate = rawDate
		};

		var viewModel = new ArticleViewModel(item, isRead: false);

		Assert.Equal(
						"Publication date unknown",
						viewModel.PublishedDateText);
	}
}
