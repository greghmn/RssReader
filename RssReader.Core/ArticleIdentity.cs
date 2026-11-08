// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
//for dealing with article ID

namespace RssReader.Core;

public static class ArticleIdentity
{
    public static string? GetId(FeedItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Id))
        {
            return item.Id;
        }

        if (!string.IsNullOrWhiteSpace(item.Link))
        {
            return item.Link;
        }

        return null;
    }
}
