
namespace RssReader.Core;

//this is for just one purpose: handling read status. Subscription, GUI logic, downloading, article parsing, etc., will be handled elsewhere.

public interface IArticleStateRepository
{
    //self-explanatory
    Task<bool> IsReadAsync(
        string feedUrl,
        string articleId);

    //note: both of the following should be idempotent! 
    Task MarkAsReadAsync(
        string feedUrl,
        string articleId);

    Task MarkAsUnreadAsync(
        string feedUrl,
        string articleId);
}
