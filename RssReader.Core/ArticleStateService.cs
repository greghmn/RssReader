//coordinate article ID and article state repo

namespace RssReader.Core;

public class ArticleStateService
{

    /*
     * there is no turning back now
     * you've woken up the demon
     * IN MEEEEEEEEEEE
    */

    private readonly IArticleStateRepository _repository;

    public ArticleStateService(IArticleStateRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> IsReadAsync(
        string feedUrl,
        FeedItem item)
    {
        string? articleId = ArticleIdentity.GetId(item);

        if (articleId is null)
        {
            return Task.FromResult(false);
        }

        return _repository.IsReadAsync(feedUrl, articleId);
    }

    public Task MarkAsReadAsync(
        string feedUrl,
        FeedItem item)
    {
        string? articleId = ArticleIdentity.GetId(item);

        if (articleId is null)
        {
            return Task.CompletedTask;
        }

        return _repository.MarkAsReadAsync(feedUrl, articleId);
    }

    public Task MarkAsUnreadAsync(
        string feedUrl,
        FeedItem item)
    {
        string? articleId = ArticleIdentity.GetId(item);

        if (articleId is null)
        {
            return Task.CompletedTask;
        }

        return _repository.MarkAsUnreadAsync(feedUrl, articleId);
    }
}
