using RssReader.App.ViewModels;
using RssReader.App.Tests.Fakes;
using RssReader.Core;

namespace RssReader.App.Tests.ViewModels;

public class MainViewModelTests
{
	private static (MainViewModel ViewModel, FakeSubscriptionRepository Repository)
			CreateViewModel()
	{
		var repository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(repository);

		var feedReader = new FakeFeedReader();

		var articleRepository = new FakeArticleRepository();

		var feedService = new FeedService(
						subscriptionService,
						feedReader,
						articleRepository);

		var articleStateRepository =
				new FakeArticleStateRepository();

		var articleStateService =
				new ArticleStateService(articleStateRepository);

		var viewModel = new MainViewModel(
				subscriptionService,
				feedService,
				articleStateService);

		return (viewModel, repository);
	}

	private static FeedSubscription CreateSubscription()
	{
		return new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		};
	}

	private static async Task<(MainViewModel ViewModel, FakeArticleStateRepository ArticleStateRepository, FakeFeedReader feedReader, FeedItem Article)>
		CreateArticleStateViewModel(bool initiallyRead = false)
	{
		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscription = CreateSubscription();

		subscriptionRepository.Subscriptions.Add(subscription);

		var article = new FeedItem
		{
			Title = "Example Article",
			Link = "https://example.com/article",
			Summary = "An example article."
		};

		var feedReader = new FakeFeedReader
		{
			Items = new List<FeedItem> { article }
		};

		var articleStateRepository = new FakeArticleStateRepository();

		if (initiallyRead)
		{
			await articleStateRepository.MarkAsReadAsync(subscription.Url, ArticleIdentity.GetId(article));
		}

		var subscriptionService = new SubscriptionService(subscriptionRepository);

		var articleRepository = new FakeArticleRepository();

		var feedService = new FeedService(
						subscriptionService,
						feedReader,
						articleRepository);

		var articleStateService = new ArticleStateService(articleStateRepository);

		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var subscriptionViewModel = new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);
		viewModel.SelectedSubscription = subscriptionViewModel;

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		viewModel.SelectedArticle = viewModel.Articles.Single();

		return (viewModel, articleStateRepository, feedReader, article);
	}

	[Fact]
	public async Task MarkAsRead_WhenRepositoryThrows_PreservesUnreadState()
	{
		var (viewModel, repository, _, _) =
				await CreateArticleStateViewModel();

		repository.MarkAsReadException =
				new InvalidOperationException("Simulated database failure");

		await viewModel.MarkSelectedArticleAsReadCommand.ExecuteAsync(null);

		Assert.False(viewModel.SelectedArticle!.IsRead);
		Assert.Equal(1, viewModel.SelectedSubscription!.UnreadCount);

		Assert.Equal(
				"Unable to update the article's read status. Please try again.",
				viewModel.ArticleStateError);
	}

	[Fact]
	public async Task MarkAsUnread_WhenRepositoryThrows_PreservesReadState()
	{
		var (viewModel, repository, _, _) =
				await CreateArticleStateViewModel(initiallyRead: true);

		repository.MarkAsUnreadException =
				new InvalidOperationException("Simulated database failure");

		await viewModel.MarkSelectedArticleAsUnreadCommand.ExecuteAsync(null);

		Assert.True(viewModel.SelectedArticle!.IsRead);
		Assert.Equal(0, viewModel.SelectedSubscription!.UnreadCount);

		Assert.Equal(
				"Unable to update the article's read status. Please try again.",
				viewModel.ArticleStateError);
	}

	[Fact]
	public async Task MarkAsRead_WhenRetrySucceeds_ClearsPreviousError()
	{
		var (viewModel, repository, feedReader, article) =
				await CreateArticleStateViewModel();

		repository.MarkAsReadException =
				new InvalidOperationException("Simulated database failure");

		// First attempt fails.
		await viewModel.MarkSelectedArticleAsReadCommand.ExecuteAsync(null);

		Assert.NotNull(viewModel.ArticleStateError);
		Assert.False(viewModel.SelectedArticle!.IsRead);

		// Remove the simulated failure.
		repository.MarkAsReadException = null;

		// Second attempt succeeds.
		await viewModel.MarkSelectedArticleAsReadCommand.ExecuteAsync(null);

		Assert.True(viewModel.SelectedArticle!.IsRead);
		Assert.Equal(0, viewModel.SelectedSubscription!.UnreadCount);
		Assert.Null(viewModel.ArticleStateError);

		Assert.True(
				await repository.IsReadAsync(
						"https://example.com/feed.xml",
						ArticleIdentity.GetId(article)));
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSuccessful_RemovesSubscription()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.True(result);
		Assert.Empty(viewModel.Subscriptions);
		Assert.Empty(repository.Subscriptions);
		Assert.Null(viewModel.SubscriptionOperationError);
		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSelected_ClearsSelection()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		viewModel.SelectedSubscription = subscriptionViewModel;

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.True(result);
		Assert.Null(viewModel.SelectedSubscription);
		Assert.False(viewModel.HasSelectedSubscription);
		Assert.True(viewModel.HasNoSelectedSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSubscriptionDoesNotExist_ReturnsFalse()
	{
		var (viewModel, _) = CreateViewModel();

		var subscriptionViewModel =
				new SubscriptionViewModel(CreateSubscription());

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.False(result);

		Assert.Equal(
				"The subscription could not be found.",
				viewModel.SubscriptionOperationError);

		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenRepositoryThrows_PreservesSubscription()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		repository.RemoveException =
				new InvalidOperationException("Simulated database failure");

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.False(result);

		Assert.Single(viewModel.Subscriptions);
		Assert.Single(repository.Subscriptions);

		Assert.Equal(
				"Unable to remove the subscription. Please try again.",
				viewModel.SubscriptionOperationError);

		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenAnotherRemovalIsInProgress_RejectsSecondAttempt()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		var removeStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRemove = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		repository.RemoveStarted = removeStarted;
		repository.AllowRemove = allowRemove;

		// Start the first removal, but don't let it finish yet.
		Task<bool> firstRemoval =
				viewModel.RemoveSubscriptionAsync(subscriptionViewModel);

		// Wait until the repository confirms removal has started.
		await removeStarted.Task;

		Assert.True(viewModel.IsRemovingSubscription);

		// Attempt a second removal while the first is still running.
		bool secondResult =
				await viewModel.RemoveSubscriptionAsync(subscriptionViewModel);

		Assert.False(secondResult);
		Assert.True(viewModel.IsRemovingSubscription);
		Assert.Single(viewModel.Subscriptions);
		Assert.Single(repository.Subscriptions);

		// Let the first removal finish.
		allowRemove.SetResult(true);

		bool firstResult = await firstRemoval;

		Assert.True(firstResult);
		Assert.Empty(viewModel.Subscriptions);
		Assert.Empty(repository.Subscriptions);
		Assert.False(viewModel.IsRemovingSubscription);
	}

	[Fact]
	public async Task RemoveSubscriptionAsync_WhenSuccessful_ClearsPreviousError()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();

		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		// Simulate an error from a previous operation.
		viewModel.SubscriptionOperationError = "Previous error.";

		bool result = await viewModel.RemoveSubscriptionAsync(
				subscriptionViewModel);

		Assert.True(result);
		Assert.Null(viewModel.SubscriptionOperationError);
		Assert.Empty(viewModel.Subscriptions);
		Assert.Empty(repository.Subscriptions);
	}

	[Fact]
	public async Task RefreshSelectedFeed_WhenReaderThrows_PreservesExistingArticles()
	{
		var (viewModel, _, feedReader, _) =
				await CreateArticleStateViewModel();

		ArticleViewModel originalArticle = viewModel.Articles.Single();

		ArticleViewModel? originalSelection = viewModel.SelectedArticle;

		feedReader.ReadException =
				new InvalidOperationException("Simulated network failure");

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		Assert.Single(viewModel.Articles);

		Assert.Same(
				originalArticle,
				viewModel.Articles.Single());

		Assert.Same(
				originalSelection,
				viewModel.SelectedArticle);

		Assert.Equal(
				"Unable to load articles. Check your connection and try again.",
				viewModel.ArticleLoadError);

		Assert.False(viewModel.IsRefreshingArticles);
		Assert.False(viewModel.IsLoadingArticles);
	}

	[Fact]
	public async Task RefreshSelectedFeed_WhenSuccessful_UpdatesArticlesAndPreservesSelection()
	{
		var (viewModel, _, feedReader, _) =
				await CreateArticleStateViewModel();

		ArticleViewModel originalArticle = viewModel.SelectedArticle!;

		var newArticle = new FeedItem
		{
			Title = "New Example Article",
			Link = "https://example.com/new-article",
			Summary = "A newly published article."
		};

		feedReader.Items = new()
		{
				originalArticle.Item,
				newArticle
		};

		viewModel.ArticleLoadError = "Previous refresh error.";

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		Assert.Equal(2, viewModel.Articles.Count);

		Assert.Equal(
				"New Example Article",
				viewModel.Articles[1].Item.Title);

		Assert.NotSame(originalArticle, viewModel.Articles[0]);

		Assert.Equal(
				originalArticle.Item.Link,
				viewModel.SelectedArticle!.Item.Link);

		Assert.Null(viewModel.ArticleLoadError);
		Assert.False(viewModel.IsRefreshingArticles);
		Assert.False(viewModel.IsLoadingArticles);
	}

	[Fact]
	public async Task RRefreshSelectedFeed_WhenArticleDisappearsFromFeed_PreservesStoredArticleAndSelection()
	{
		var (viewModel, _, feedReader, _) =
				await CreateArticleStateViewModel();

		var replacementArticle = new FeedItem
		{
			Title = "Replacement Article",
			Link = "https://example.com/replacement",
			Summary = "Another article."
		};

		feedReader.Items = new()
		{
				replacementArticle
		};

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		Assert.Equal(2, viewModel.Articles.Count);

		Assert.Contains(viewModel.Articles,
				article => article.Item.Link == "https://example.com/article");

		Assert.Contains(viewModel.Articles,
				article => article.Item.Link == "https://example.com/replacement");

		Assert.NotNull(viewModel.SelectedArticle);

		Assert.Equal(
				"https://example.com/article",
				viewModel.SelectedArticle.Item.Link);

		Assert.True(viewModel.HasSelectedArticle);
		Assert.False(viewModel.HasNoSelectedArticle);
		Assert.False(viewModel.IsRefreshingArticles);
	}

	[Fact]
	public async Task RefreshSelectedFeed_WhenFeedIsEmpty_PreservesStoredArticles()
	{
		var (viewModel, _, feedReader, _) =
				await CreateArticleStateViewModel();

		feedReader.Items = new();

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		// An empty feed must not erase previously saved articles.
		Assert.Single(viewModel.Articles);

		Assert.Equal(
				"https://example.com/article",
				viewModel.Articles[0].Item.Link);

		// The previously selected article is still available.
		Assert.NotNull(viewModel.SelectedArticle);

		Assert.Equal(
				"https://example.com/article",
				viewModel.SelectedArticle.Item.Link);

		Assert.True(viewModel.HasSelectedArticle);
		Assert.False(viewModel.HasNoSelectedArticle);

		// The UI must not report an empty archive.
		Assert.False(viewModel.HasNoArticles);
		Assert.False(viewModel.HasArticlesAwaitingSelection);

		// The network retrieval succeeded, even though it returned no items.
		Assert.Null(viewModel.ArticleLoadError);

		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
	}

	[Fact]
	public async Task HasArticlesAwaitingSelection_ReflectsCurrentViewModelState()
	{
		var (viewModel, _, _, _) =
				await CreateArticleStateViewModel();

		// A selected article means no selection prompt is needed.
		Assert.False(viewModel.HasArticlesAwaitingSelection);

		// Deselecting the article should show the prompt.
		viewModel.SelectedArticle = null;
		Assert.True(viewModel.HasArticlesAwaitingSelection);

		// Deselecting the subscription should hide the prompt again.
		viewModel.SelectedSubscription = null;
		Assert.False(viewModel.HasArticlesAwaitingSelection);
	}

	[Fact]
	public async Task HasArticlesAwaitingSelection_IsFalseWhileLoading()
	{
		var (viewModel, _, _, _) =
				await CreateArticleStateViewModel();

		viewModel.IsLoadingArticles = true;

		Assert.False(viewModel.HasArticlesAwaitingSelection);
	}

	[Fact]
	public async Task HasArticlesAwaitingSelection_IsFalseWhenRetrievalFails()
	{
		var (viewModel, _, _, _) =
				await CreateArticleStateViewModel();

		viewModel.SelectedArticle = null;
		viewModel.ArticleLoadError = "Unable to load articles.";

		Assert.False(viewModel.HasArticlesAwaitingSelection);
	}

	[Fact]
	public async Task RenameSubscriptionAsync_WhenSuccessful_UpdatesTitle()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();
		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		viewModel.SubscriptionRenameError = "Previous error.";

		bool result = await viewModel.RenameSubscriptionAsync(
				subscriptionViewModel,
				"Renamed Feed");

		Assert.True(result);
		Assert.Equal("Renamed Feed", subscriptionViewModel.Title);
		Assert.Equal("Renamed Feed", subscription.Title);
		Assert.Equal("Renamed Feed", repository.Subscriptions.Single().Title);

		Assert.Null(viewModel.SubscriptionRenameError);
		Assert.False(viewModel.IsRenamingSubscription);
	}

	[Fact]
	public async Task RenameSubscriptionAsync_WhenSubscriptionDoesNotExist_ReturnsFalse()
	{
		var (viewModel, _) = CreateViewModel();

		var subscriptionViewModel =
				new SubscriptionViewModel(CreateSubscription());

		bool result = await viewModel.RenameSubscriptionAsync(
				subscriptionViewModel,
				"Renamed Feed");

		Assert.False(result);
		Assert.Equal("Example Feed", subscriptionViewModel.Title);

		Assert.Equal(
				"The subscription could not be found.",
				viewModel.SubscriptionRenameError);

		Assert.False(viewModel.IsRenamingSubscription);
	}

	[Fact]
	public async Task RenameSubscriptionAsync_WhenRepositoryThrows_PreservesTitle()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();
		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		repository.UpdateTitleException =
				new InvalidOperationException("Simulated database failure");

		bool result = await viewModel.RenameSubscriptionAsync(
				subscriptionViewModel,
				"Renamed Feed");

		Assert.False(result);

		Assert.Equal("Example Feed", subscriptionViewModel.Title);
		Assert.Equal("Example Feed", subscription.Title);

		Assert.Equal(
				"Unable to rename the subscription. Please try again.",
				viewModel.SubscriptionRenameError);

		Assert.False(viewModel.IsRenamingSubscription);
	}

	[Fact]
	public async Task RenameSubscriptionAsync_WhenAnotherRenameIsInProgress_RejectsSecondAttempt()
	{
		var (viewModel, repository) = CreateViewModel();

		FeedSubscription subscription = CreateSubscription();
		repository.Subscriptions.Add(subscription);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		var updateStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowUpdate = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		repository.UpdateTitleStarted = updateStarted;
		repository.AllowUpdateTitle = allowUpdate;

		// Start the first rename, but don't let it finish yet.
		Task<bool> firstRename =
				viewModel.RenameSubscriptionAsync(
						subscriptionViewModel,
						"First Rename");

		// Wait until the repository confirms the operation has started.
		await updateStarted.Task;

		Assert.True(viewModel.IsRenamingSubscription);

		// Attempt a second rename while the first is still running.
		bool secondResult = await viewModel.RenameSubscriptionAsync(
				subscriptionViewModel,
				"Second Rename");

		Assert.False(secondResult);
		Assert.True(viewModel.IsRenamingSubscription);

		Assert.Equal("Example Feed", subscriptionViewModel.Title);
		Assert.Equal("Example Feed", subscription.Title);

		// Allow the first rename to finish.
		allowUpdate.SetResult(true);

		bool firstResult = await firstRename;

		Assert.True(firstResult);
		Assert.Equal("First Rename", subscriptionViewModel.Title);
		Assert.Equal("First Rename", subscription.Title);
		Assert.False(viewModel.IsRenamingSubscription);
	}

	//test cached articles when the network fails 
	[Fact]
	public async Task SelectSubscription_WhenReaderThrows_DisplaysStoredArticles()
	{
		var subscription = CreateSubscription();

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(subscription);

		var articleRepository = new FakeArticleRepository();

		await articleRepository.SaveAsync(
				subscription.Url,
				new[]
				{
						new FeedItem
						{
								Id = "cached-article-1",
								Title = "Saved for Offline Reading",
								Link = "https://example.com/cached-article",
								Summary = "An article retrieved earlier."
						}
				});

		var feedReader = new FakeFeedReader
		{
			ReadException =
						new InvalidOperationException("Simulated network failure")
		};

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				articleRepository);

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());

		var viewModel = new MainViewModel(
				subscriptionService,
				feedService,
				articleStateService);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);

		// Selecting the subscription triggers asynchronous loading.
		viewModel.SelectedSubscription = subscriptionViewModel;

		// Wait for the initial load, including the failed network request.
		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		// The cached article must remain visible.
		Assert.Single(viewModel.Articles);

		Assert.Equal(
				"Saved for Offline Reading",
				viewModel.Articles[0].Item.Title);

		Assert.Equal(
				"cached-article-1",
				viewModel.Articles[0].Item.Id);

		// The failed network request must still be reported.
		Assert.Equal(
				"Unable to load articles. Check your connection and try again.",
				viewModel.ArticleLoadError);

		// Cached articles must retain their read/unread information.
		Assert.False(viewModel.Articles[0].IsRead);
		Assert.Equal(1, subscriptionViewModel.UnreadCount);

		// Loading indicators must return to their idle state.
		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
	}

	[Fact]
	public async Task SelectSubscription_WhenPreviousLoadFinishesLater_KeepsCurrentFeed()
	{
		var feedA = new FeedSubscription
		{
			Url = "https://example.com/feed-a.xml",
			Title = "Feed A"
		};

		var feedB = new FeedSubscription
		{
			Url = "https://example.com/feed-b.xml",
			Title = "Feed B"
		};

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(feedA);
		subscriptionRepository.Subscriptions.Add(feedB);

		var feedAReadStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowFeedARead = new TaskCompletionSource<List<FeedItem>>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, cancellationToken) =>
			{
				if (url == feedA.Url)
				{
					feedAReadStarted.TrySetResult(true);

					return await allowFeedARead.Task.WaitAsync(
							cancellationToken);
				}

				if (url == feedB.Url)
				{
					return new List<FeedItem>
								{
										new()
										{
												Id = "article-b",
												Title = "Article from Feed B",
												Link = "https://example.com/article-b"
										}
								};
				}

				throw new InvalidOperationException(
						$"Unexpected feed URL: {url}");
			}
		};

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());

		var viewModel = new MainViewModel(
				subscriptionService,
				feedService,
				articleStateService);

		var subscriptionA = new SubscriptionViewModel(feedA);
		var subscriptionB = new SubscriptionViewModel(feedB);

		viewModel.Subscriptions.Add(subscriptionA);
		viewModel.Subscriptions.Add(subscriptionB);

		// Start loading Feed A, but keep its network request pending.
		viewModel.SelectedSubscription = subscriptionA;

		Assert.NotNull(viewModel.InitialArticleLoadTask);

		Task feedALoad = viewModel.InitialArticleLoadTask;

		await feedAReadStarted.Task;

		// Switch to Feed B while Feed A is still loading.
		viewModel.SelectedSubscription = subscriptionB;

		Assert.NotNull(viewModel.InitialArticleLoadTask);

		await viewModel.InitialArticleLoadTask;

		// Feed B should now be fully loaded.
		Assert.Single(viewModel.Articles);
		Assert.Equal(
				"Article from Feed B",
				viewModel.Articles[0].Item.Title);

		Assert.False(viewModel.IsLoadingArticles);
		Assert.Null(viewModel.ArticleLoadError);

		// Allow the older Feed A request to finish afterward.
		allowFeedARead.SetResult(new List<FeedItem>
		{
				new()
				{
						Id = "article-a",
						Title = "Article from Feed A",
						Link = "https://example.com/article-a"
				}
		});

		await feedALoad;

		// Feed A must not overwrite the currently selected feed.
		Assert.Same(subscriptionB, viewModel.SelectedSubscription);

		Assert.Single(viewModel.Articles);

		Assert.Equal(
				"Article from Feed B",
				viewModel.Articles[0].Item.Title);

		Assert.Equal(1, subscriptionB.UnreadCount);

		Assert.Null(viewModel.ArticleLoadError);
		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
	}

	[Fact]
	public async Task SelectSubscription_WhenPreviousLoadFailsLater_DoesNotShowStaleError()
	{
		var feedA = new FeedSubscription
		{
			Url = "https://example.com/feed-a.xml",
			Title = "Feed A"
		};

		var feedB = new FeedSubscription
		{
			Url = "https://example.com/feed-b.xml",
			Title = "Feed B"
		};

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(feedA);
		subscriptionRepository.Subscriptions.Add(feedB);

		var feedAReadStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowFeedARead = new TaskCompletionSource<List<FeedItem>>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, cancellationToken) =>
			{
				if (url == feedA.Url)
				{
					feedAReadStarted.TrySetResult(true);

					return await allowFeedARead.Task.WaitAsync(
							cancellationToken);
				}

				if (url == feedB.Url)
				{
					return new List<FeedItem>
								{
										new()
										{
												Id = "article-b",
												Title = "Article from Feed B",
												Link = "https://example.com/article-b"
										}
								};
				}

				throw new InvalidOperationException(
						$"Unexpected feed URL: {url}");
			}
		};

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());

		var viewModel = new MainViewModel(
				subscriptionService,
				feedService,
				articleStateService);

		var subscriptionA = new SubscriptionViewModel(feedA);
		var subscriptionB = new SubscriptionViewModel(feedB);

		viewModel.Subscriptions.Add(subscriptionA);
		viewModel.Subscriptions.Add(subscriptionB);

		// Start loading Feed A, but keep its network request pending.
		viewModel.SelectedSubscription = subscriptionA;

		Assert.NotNull(viewModel.InitialArticleLoadTask);

		Task feedALoad = viewModel.InitialArticleLoadTask;

		await feedAReadStarted.Task;

		// Switch to Feed B while Feed A is still loading.
		viewModel.SelectedSubscription = subscriptionB;

		Assert.NotNull(viewModel.InitialArticleLoadTask);

		await viewModel.InitialArticleLoadTask;

		// Feed B should now be fully loaded.
		Assert.Single(viewModel.Articles);
		Assert.Equal(
				"Article from Feed B",
				viewModel.Articles[0].Item.Title);

		Assert.False(viewModel.IsLoadingArticles);
		Assert.Null(viewModel.ArticleLoadError);

		// Allow the older Feed A request to finish afterward.
		allowFeedARead.SetException(
				new InvalidOperationException("Simulated Feed A failure"));

		await feedALoad;

		// Feed A must not overwrite the currently selected feed.
		Assert.Same(subscriptionB, viewModel.SelectedSubscription);

		Assert.Single(viewModel.Articles);

		Assert.Equal(
				"Article from Feed B",
				viewModel.Articles[0].Item.Title);

		Assert.Equal(1, subscriptionB.UnreadCount);

		Assert.Null(viewModel.ArticleLoadError);
		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
	}

	[Fact]
	public async Task RefreshSelectedFeed_WhenAlreadyRefreshing_DoesNotStartAnotherRequest()
	{
		var subscription = CreateSubscription();

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(subscription);

		int readCount = 0;

		var refreshStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRefresh = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, cancellationToken) =>
			{
				readCount++;

				// Let the initial subscription load complete normally.
				if (readCount == 1)
				{
					return new List<FeedItem>();
				}

				refreshStarted.TrySetResult(true);

				await allowRefresh.Task.WaitAsync(cancellationToken);

				return new List<FeedItem>();
			}
		};

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());

		var viewModel = new MainViewModel(
				subscriptionService,
				feedService,
				articleStateService);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);
		viewModel.SelectedSubscription = subscriptionViewModel;

		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		Assert.Equal(1, readCount);

		// Start a refresh and keep it pending.
		Task firstRefresh =
				viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		await refreshStarted.Task;

		Assert.True(viewModel.IsRefreshingArticles);

		// Attempt another refresh while the first is still running.
		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		// No second network request should have started.
		Assert.Equal(2, readCount);

		// Release the first refresh.
		allowRefresh.SetResult(true);
		await firstRefresh;

		Assert.False(viewModel.IsRefreshingArticles);
		Assert.False(viewModel.IsLoadingArticles);
		Assert.Null(viewModel.ArticleLoadError);
	}
}