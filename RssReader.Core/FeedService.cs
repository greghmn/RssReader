
namespace RssReader.Core;

public class FeedService
{
	private readonly SubscriptionService _subscriptionService;
	private readonly IFeedReader _feedReader;
	private readonly IArticleRepository _articleRepository;

	public FeedService(
			SubscriptionService subscriptionService,
			IFeedReader feedReader,
			IArticleRepository articleRepository)
	{
		_subscriptionService = subscriptionService;
		_feedReader = feedReader;
		_articleRepository = articleRepository;
	}


	public async Task<List<FeedItem>> GetItemsAsync(string feedUrl)
	{
		bool isSubscribed =
				await _subscriptionService.IsSubscribedAsync(feedUrl);

		if (!isSubscribed)
		{
			throw new InvalidOperationException(
					"Cannot read a feed that is not subscribed to.");
		}

		List<FeedItem> articles =
				await _feedReader.ReadAsync(feedUrl);

		try
		{
			await _articleRepository.SaveAsync(feedUrl, articles);
		}
		catch (Exception ex)
		{
			throw new ArticleStorageException(
					"Unable to save retrieved articles.",
					ArticleStorageOperation.Write,
					ex);
		}

		return articles;
	}

	//for retrieving cached articles
	public async Task<List<FeedItem>> GetStoredItemsAsync(string feedUrl)
	{
		try
		{
			return await _articleRepository.GetByFeedAsync(feedUrl);
		}
		catch (Exception ex)
		{
			throw new ArticleStorageException(
					"Unable to read saved articles.",
					ArticleStorageOperation.Read,
					ex);
		}
	}
}
