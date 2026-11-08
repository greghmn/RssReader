// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using RssReader.App.ViewModels;
using RssReader.App.Tests.Fakes;
using RssReader.Core;

namespace RssReader.App.Tests.ViewModels;

public class SubscriptionViewModelTests
{

	[Theory]
	[InlineData("https://example.com/feed.xml")]
	[InlineData("http://example.com/feed.xml")]

	public void FeedUri_WithValidHttpUrl_ReturnsUri(string url)
	{
		var subscription = new FeedSubscription
		{
			Url = url,
			Title = "Test feed"
		};

		var viewModel = new SubscriptionViewModel(subscription);

		Assert.Equal(new Uri(url), viewModel.FeedUri);
	}

	[Theory]
	[InlineData("not a valid url")]
	[InlineData("feeds/example.xml")]
	[InlineData("ftp://example.com/feed.xml")]

	public void FeedUri_WithInvalidUrl_ReturnsNull(string url)
	{
		var subscription = new FeedSubscription
		{
			Url = url,
			Title = "Test feed"
		};

		var viewModel = new SubscriptionViewModel(subscription);

		Assert.Null(viewModel.FeedUri);
	}
}
