
using RssReader.Core;

FeedReader reader = new();

List<FeedItem> items = await reader.ReadAsync(
    "https://feeds.bbci.co.uk/news/rss.xml");

foreach (FeedItem item in items)
{
    Console.WriteLine(item.Title);
    Console.WriteLine(item.Link);
    Console.WriteLine();
}
