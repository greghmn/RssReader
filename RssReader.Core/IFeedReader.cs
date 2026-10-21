namespace RssReader.Core;

public interface IFeedReader
{
    Task<List<FeedItem>> ReadAsync(string feedUrl);
}
