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
