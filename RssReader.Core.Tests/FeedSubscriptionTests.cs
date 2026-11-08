// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.

using RssReader.Core;
using Xunit;

namespace RssReader.Core.Tests;

public class FeedSubscriptionTests
{
    [Fact]
    public void CreateSubscription_StoresProvidedValues()
    {
        FeedSubscription subscription = new()
        {
            Url = "https://example.com/podcast.xml",
            Title = "My Favorite Podcast"
        };

        Assert.Equal(
            "https://example.com/podcast.xml",
            subscription.Url);

        Assert.Equal(
            "My Favorite Podcast",
            subscription.Title);

        Assert.NotEqual(
            default(DateTimeOffset),
            subscription.DateAdded);
    }
}
