// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using RssReader.App.Tests.Fakes;
using RssReader.App.ViewModels;
using RssReader.Core;
using System.Runtime.Intrinsics.Arm;
using RssReader.App.Results;

namespace RssReader.App.Tests.ViewModels;

public class MainViewModelTests
{
	private static (MainViewModel ViewModel, FakeSubscriptionRepository Repository) CreateViewModel()
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

		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

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
	private static async Task<(MainViewModel ViewModel, FakeArticleStateRepository ArticleStateRepository, FakeFeedReader feedReader, FeedItem Article)> CreateArticleStateViewModel(bool initiallyRead = false)
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
			string articleId = Assert.IsType<string>(ArticleIdentity.GetId(article));

			await articleStateRepository.MarkAsReadAsync(subscription.Url, articleId);
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

		string articleId = Assert.IsType<string>(ArticleIdentity.GetId(article));

		Assert.True(
						await repository.IsReadAsync(
										"https://example.com/feed.xml",
										articleId));
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


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

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


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

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


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

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


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

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
	//verify that a failed save preserves articles and timestamps
	[Fact]
	public async Task RefreshSelectedFeed_WhenArticleSaveFails_PreservesArticlesAndTimestamp()
	{
		var subscription = CreateSubscription();

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(subscription);

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			Items = new List<FeedItem>
				{
						new()
						{
								Id = "original-article",
								Title = "Original Article",
								Link = "https://example.com/original"
						}
				}
		};

		var articleRepository = new FakeArticleRepository();

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				articleRepository);

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);
		viewModel.SelectedSubscription = subscriptionViewModel;

		// Complete the initial successful load.
		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		Assert.Single(viewModel.Articles);
		Assert.Equal(1, subscriptionRepository.UpdateLastRetrievedCount);

		// Remember the original UI state and retrieval timestamp.
		ArticleViewModel originalArticle = viewModel.Articles.Single();
		DateTimeOffset? originalTimestamp = subscription.LastRetrieved;

		Assert.NotNull(originalTimestamp);

		viewModel.SelectedArticle = originalArticle;

		// The publisher now offers another article.
		feedReader.Items = new List<FeedItem>
		{
				new()
				{
						Id = "new-article",
						Title = "New Article",
						Link = "https://example.com/new"
				}
		};

		// Simulate SQLite failing to save the new articles.
		articleRepository.SaveException =
				new InvalidOperationException("Simulated database failure");

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		// The existing article and selection must survive.
		Assert.Single(viewModel.Articles);
		Assert.Same(originalArticle, viewModel.Articles.Single());
		Assert.Same(originalArticle, viewModel.SelectedArticle);

		// The failed operation must be reported.
		Assert.Equal(
				"Unable to save retrieved articles. Your previously saved articles are still available.",
				viewModel.ArticleLoadError);

		// A failed save must not count as a successful retrieval.
		Assert.Equal(1, subscriptionRepository.UpdateLastRetrievedCount);
		Assert.Equal(originalTimestamp, subscription.LastRetrieved);
		Assert.Equal(originalTimestamp, subscriptionViewModel.LastRetrieved);

		// Loading indicators must return to their irdle state.
		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
	}
	[Fact]
	public async Task RefreshSelectedFeed_WhenArchiveReadFails_PreservesExistingArticles()
	{
		var subscription = CreateSubscription();

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(subscription);

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			Items = new List<FeedItem>
				{
						new()
						{
								Id = "original-article",
								Title = "Original Article",
								Link = "https://example.com/original"
						}
				}
		};

		var articleRepository = new FakeArticleRepository();

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				articleRepository);

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);
		viewModel.SelectedSubscription = subscriptionViewModel;

		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		ArticleViewModel originalArticle = viewModel.Articles.Single();
		viewModel.SelectedArticle = originalArticle;

		DateTimeOffset? originalTimestamp = subscription.LastRetrieved;
		int originalUpdateCount =
				subscriptionRepository.UpdateLastRetrievedCount;

		Assert.NotNull(originalTimestamp);

		// The network now returns another article.
		feedReader.Items = new List<FeedItem>
		{
				new()
				{
						Id = "new-article",
						Title = "New Article",
						Link = "https://example.com/new"
				}
		};

		// Simulate failure when reading the saved archive.
		articleRepository.GetException =
				new InvalidOperationException("Simulated archive read failure");

		await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);

		// The newly retrieved article was saved successfully.
		articleRepository.GetException = null;

		List<FeedItem> storedArticles =
				await articleRepository.GetByFeedAsync(subscription.Url);

		Assert.Equal(2, storedArticles.Count);

		Assert.Contains(
				storedArticles,
				article => article.Id == "original-article");

		Assert.Contains(
				storedArticles,
				article => article.Id == "new-article");

		// Existing articles and selection must survive.
		Assert.Single(viewModel.Articles);
		Assert.Same(originalArticle, viewModel.Articles.Single());
		Assert.Same(originalArticle, viewModel.SelectedArticle);

		// The failure must be visible.
		Assert.Equal(
				"Unable to read saved articles from local storage.",
				viewModel.ArticleLoadError);

		// No completed refresh should be recorded.
		Assert.Equal(
				originalUpdateCount,
				subscriptionRepository.UpdateLastRetrievedCount);

		Assert.Equal(originalTimestamp, subscription.LastRetrieved);
		Assert.Equal(originalTimestamp, subscriptionViewModel.LastRetrieved);

		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
	}
	//intial archive read fails, network retrieval still runs, and retrieved article is saved and displayed
	[Fact]
	public async Task SelectSubscription_WhenInitialArchiveReadFails_StillRetrievesFeed()
	{
		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		};

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(subscription);

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			Items = new List<FeedItem>
				{
						new()
						{
								Id = "article-1",
								Title = "Retrieved Article",
								Link = "https://example.com/article-1"
						}
				}
		};

		var articleRepository = new FakeArticleRepository
		{
			FailFirstGet = true
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				articleRepository);

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);
		viewModel.SelectedSubscription = subscriptionViewModel;

		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		Assert.True(articleRepository.GetCount >= 2);

		ArticleViewModel article = Assert.Single(viewModel.Articles);

		Assert.Equal("Retrieved Article", article.Item.Title);
		Assert.Null(viewModel.ArticleLoadError);

		Assert.NotNull(subscription.LastRetrieved);
		Assert.False(viewModel.IsLoadingArticles);
	}
	//scenario: archive is persistently unreadable
	[Fact]
	public async Task SelectSubscription_WhenArchiveRemainsUnreadable_ReportsStorageError()
	{
		var subscription = new FeedSubscription
		{
			Url = "https://example.com/feed.xml",
			Title = "Example Feed"
		};

		var subscriptionRepository = new FakeSubscriptionRepository();
		subscriptionRepository.Subscriptions.Add(subscription);

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			Items = new List<FeedItem>
				{
						new()
						{
								Id = "article-1",
								Title = "Retrieved Article",
								Link = "https://example.com/article-1"
						}
				}
		};

		var articleRepository = new FakeArticleRepository
		{
			GetException = new InvalidOperationException(
						"Simulated persistent archive read failure")
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				articleRepository);

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var subscriptionViewModel =
				new SubscriptionViewModel(subscription);

		viewModel.Subscriptions.Add(subscriptionViewModel);
		viewModel.SelectedSubscription = subscriptionViewModel;

		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		// Both archive reads should have been attempted.
		Assert.Equal(2, articleRepository.GetCount);

		// The archive failure must be reported accurately.
		Assert.Equal(
				"Unable to read saved articles from local storage. Showing recently retrieved articles instead.",
				viewModel.ArticleLoadError);

		// The UI should remain in a consistent state.
		ArticleViewModel article = Assert.Single(viewModel.Articles);

		Assert.Equal("article-1", article.Item.Id);
		Assert.Equal("Retrieved Article", article.Item.Title);
		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);

		// The full loading workflow did not complete.
		Assert.Null(subscription.LastRetrieved);
		Assert.Equal(
				0,
				subscriptionRepository.UpdateLastRetrievedCount);
	}
	[Fact]
	public async Task LoadSubscriptionsAsync_PreservesSelectionAndArticles()
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
								Title = "Saved Article",
								Link = "https://example.com/cached-article"
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


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel selected =
				Assert.Single(viewModel.Subscriptions);

		viewModel.SelectedSubscription = selected;

		Assert.NotNull(viewModel.InitialArticleLoadTask);
		await viewModel.InitialArticleLoadTask;

		ArticleViewModel article =
				Assert.Single(viewModel.Articles);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/new-feed.xml",
			Title = "New Feed"
		});

		await viewModel.LoadSubscriptionsAsync();

		Assert.Equal(2, viewModel.Subscriptions.Count);

		Assert.Same(selected, viewModel.SelectedSubscription);
		Assert.Same(selected, viewModel.Subscriptions[0]);

		Assert.Same(article, Assert.Single(viewModel.Articles));
	}
	[Fact]
	public async Task ExportOpmlAsync_ExportsPersistedSubscriptions()
	{
		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/first.xml",
			Title = "First Feed"
		});

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = "https://example.com/second.xml",
			Title = "Second & Third"
		});

		var feedService = new FeedService(
				subscriptionService,
				new FakeFeedReader(),
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		string xml = await viewModel.ExportOpmlAsync();

		var document = System.Xml.Linq.XDocument.Parse(xml);

		var outlines = document
				.Descendants("outline")
				.ToList();

		Assert.Equal(2, outlines.Count);

		Assert.Equal(
				"https://example.com/first.xml",
				(string?)outlines[0].Attribute("xmlUrl"));

		Assert.Equal(
				"First Feed",
				(string?)outlines[0].Attribute("title"));

		Assert.Equal(
				"https://example.com/second.xml",
				(string?)outlines[1].Attribute("xmlUrl"));

		Assert.Equal(
				"Second & Third",
				(string?)outlines[1].Attribute("title"));

		Assert.Empty(viewModel.Subscriptions);
	}
	[Fact]
	public async Task SelectingAnotherSubscription_CancelsPreviousFeedRequest()
	{
		const string firstUrl = "https://example.com/first.xml";
		const string secondUrl = "https://example.com/second.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = firstUrl,
			Title = "First Feed"
		});

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = secondUrl,
			Title = "Second Feed"
		});

		var firstReadStarted =
				new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);

		var firstReadCanceled =
				new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				if (url == firstUrl)
				{
					firstReadStarted.TrySetResult(true);

					try
					{
						await Task.Delay(
								Timeout.InfiniteTimeSpan,
								token);
					}
					catch (OperationCanceledException)
					{
						firstReadCanceled.TrySetResult(true);
						throw;
					}
				}

				return new List<FeedItem>();
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel firstSubscription =
				Assert.Single(
						viewModel.Subscriptions,
						item => item.Item.Url == firstUrl);

		SubscriptionViewModel secondSubscription =
				Assert.Single(
						viewModel.Subscriptions,
						item => item.Item.Url == secondUrl);

		viewModel.SelectedSubscription = firstSubscription;

		await firstReadStarted.Task.WaitAsync(
				TimeSpan.FromSeconds(5));

		Task firstLoad = Assert.IsAssignableFrom<Task>(viewModel.InitialArticleLoadTask);

		viewModel.SelectedSubscription = secondSubscription;

		await firstReadCanceled.Task.WaitAsync(
				TimeSpan.FromSeconds(5));

		await firstLoad;

		Assert.Same(
				secondSubscription,
				viewModel.SelectedSubscription);

		Assert.Null(viewModel.ArticleLoadError);
	}
	[Fact]
	public async Task DeselectingSubscription_CancelsPendingFeedRequest()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var readStarted =
				new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);

		var readCanceled =
				new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				readStarted.TrySetResult(true);

				try
				{
					await Task.Delay(
							Timeout.InfiniteTimeSpan,
							token);
				}
				catch (OperationCanceledException)
				{
					readCanceled.TrySetResult(true);
					throw;
				}

				return new List<FeedItem>();
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		viewModel.SelectedSubscription = subscription;

		await readStarted.Task.WaitAsync(
				TimeSpan.FromSeconds(5));

		Task pendingLoad = Assert.IsAssignableFrom<Task>(viewModel.InitialArticleLoadTask);

		viewModel.SelectedSubscription = null;

		await readCanceled.Task.WaitAsync(
				TimeSpan.FromSeconds(5));

		await pendingLoad;

		Assert.Null(viewModel.SelectedSubscription);
		Assert.Empty(viewModel.Articles);
		Assert.Null(viewModel.ArticleLoadError);

		Assert.False(viewModel.IsLoadingArticles);
		Assert.False(viewModel.IsRefreshingArticles);
		Assert.False(viewModel.HasNoArticles);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_PreventsDuplicateRequests()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var readStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRead = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		int readCount = 0;

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				readCount++;

				readStarted.TrySetResult(true);

				await allowRead.Task.WaitAsync(token);

				return new List<FeedItem>();
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		Task firstRefresh =
				viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		try
		{
			await readStarted.Task.WaitAsync(
					TimeSpan.FromSeconds(5));

			Task secondRefresh =
					viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

			await secondRefresh;

			Assert.Equal(1, readCount);
		}
		finally
		{
			allowRead.TrySetResult(true);
			await firstRefresh;
		}

		Assert.Equal(1, readCount);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_AllowsSubsequentRefresh()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		int readCount = 0;

		var feedReader = new FakeFeedReader
		{
			ReadHandler = (url, token) =>
			{
				readCount++;

				return Task.FromResult(new List<FeedItem>());
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		await viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		await viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		Assert.Equal(2, readCount);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_ReportsSuccess()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var readStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRead = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				readStarted.TrySetResult(true);

				await allowRead.Task.WaitAsync(token);

				return new List<FeedItem>();
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		Task refreshTask =
				viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		try
		{
			await readStarted.Task.WaitAsync(
					TimeSpan.FromSeconds(5));

			Assert.True(subscription.IsRefreshing);
			Assert.Null(subscription.RefreshStatus);
			Assert.Null(subscription.RefreshError);
		}
		finally
		{
			allowRead.TrySetResult(true);
			await refreshTask;
		}

		Assert.False(subscription.IsRefreshing);
		Assert.Equal(
				"Refreshed successfully.",
				subscription.RefreshStatus);
		Assert.Null(subscription.RefreshError);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_WhenSaveFails_ReportsStorageError()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();
		var subscriptionService =
						new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var articleRepository = new FakeArticleRepository
		{
			SaveException =
								new InvalidOperationException("Simulated database failure")
		};

		var feedService = new FeedService(
						subscriptionService,
						new FakeFeedReader(),
						articleRepository);

		var articleStateService = new ArticleStateService(
						new FakeArticleStateRepository());

		var viewModel = new MainViewModel(
						subscriptionService,
						feedService,
						articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
						Assert.Single(viewModel.Subscriptions);

		await viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		Assert.Equal(
						"Unable to save retrieved articles to local storage.",
						subscription.RefreshError);

		Assert.Null(subscription.RefreshStatus);
		Assert.Null(subscription.LastRetrieved);
		Assert.False(subscription.IsRefreshing);

		Assert.Equal(
						0,
						subscriptionRepository.UpdateLastRetrievedCount);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_ReportsFailure()
	{
		const string firstUrl = "https://example.com/first.xml";
		const string secondUrl = "https://example.com/second.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = firstUrl,
			Title = "First Feed"
		});

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = secondUrl,
			Title = "Second Feed"
		});

		var feedReader = new FakeFeedReader
		{
			ReadHandler = (url, token) =>
			{
				if (url == secondUrl)
				{
					throw new HttpRequestException(
							"Simulated network failure.");
				}

				return Task.FromResult(new List<FeedItem>());
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel firstSubscription =
				Assert.Single(
						viewModel.Subscriptions,
						item => item.Item.Url == firstUrl);

		SubscriptionViewModel secondSubscription =
				Assert.Single(
						viewModel.Subscriptions,
						item => item.Item.Url == secondUrl);

		viewModel.SelectedSubscription = firstSubscription;

		Task initialLoad = Assert.IsAssignableFrom<Task>(viewModel.InitialArticleLoadTask);

		await initialLoad;

		await viewModel.RefreshSubscriptionInBackgroundAsync(
				secondSubscription);

		Assert.False(secondSubscription.IsRefreshing);
		Assert.Null(secondSubscription.RefreshStatus);

		Assert.Equal(
				"Unable to refresh feed. Check your connection and try again.",
				secondSubscription.RefreshError);

		Assert.Same(
				firstSubscription,
				viewModel.SelectedSubscription);

		Assert.Null(viewModel.ArticleLoadError);
		Assert.Null(firstSubscription.RefreshError);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_UpdatesUnreadCount()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();
		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var items = new List<FeedItem>
		{
				new()
				{
						FeedUrl = feedUrl,
						Id = "article-1",
						Title = "First article"
				},
				new()
				{
						FeedUrl = feedUrl,
						Id = "article-2",
						Title = "Second article"
				}
		};

		var feedReader = new FakeFeedReader
		{
			ReadHandler = (url, token) =>
					Task.FromResult(items)
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());

		await articleStateService.MarkAsReadAsync(
				feedUrl,
				items[0]);


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		await viewModel.RefreshSubscriptionInBackgroundAsync(
				subscription);

		Assert.Equal(1, subscription.UnreadCount);
		Assert.False(subscription.IsRefreshing);
		Assert.Null(subscription.RefreshError);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_DoesNotUpdateRemovedSubscription()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var readStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRead = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				readStarted.TrySetResult(true);

				await allowRead.Task.WaitAsync(token);

				return new List<FeedItem>();
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		DateTimeOffset? originalLastRetrieved =
				subscription.LastRetrieved;

		Task refreshTask =
				viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		try
		{
			await readStarted.Task.WaitAsync(
					TimeSpan.FromSeconds(5));

			Assert.True(subscription.IsRefreshing);

			bool removed =
					await viewModel.RemoveSubscriptionAsync(subscription);

			Assert.True(removed);
			Assert.Empty(viewModel.Subscriptions);
		}
		finally
		{
			allowRead.TrySetResult(true);
			await refreshTask;
		}

		Assert.False(subscription.IsRefreshing);

		Assert.Equal(
				originalLastRetrieved,
				subscription.LastRetrieved);

		Assert.Null(subscription.RefreshStatus);
		Assert.Null(subscription.RefreshError);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_WhenRemovedAndRequestFails_DoesNotReportError()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();
		var subscriptionService =
						new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var readStarted = new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);

		var allowFailure = new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				readStarted.TrySetResult(true);

				await allowFailure.Task;

				throw new HttpRequestException(
								"Simulated delayed network failure.");
			}
		};

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

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
						Assert.Single(viewModel.Subscriptions);

		Task refreshTask =
						viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		try
		{
			await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

			bool removed =
							await viewModel.RemoveSubscriptionAsync(subscription);

			Assert.True(removed);
			Assert.Empty(viewModel.Subscriptions);
		}
		finally
		{
			allowFailure.TrySetResult(true);
			await refreshTask;
		}

		Assert.False(subscription.IsRefreshing);
		Assert.Null(subscription.RefreshError);
		Assert.Null(subscription.RefreshStatus);
		Assert.Null(subscription.LastRetrieved);

		Assert.Equal(
						0,
						subscriptionRepository.UpdateLastRetrievedCount);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_SkipsUpdatesDuringRemoval()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository
		{
			RemoveStarted = new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously),

			AllowRemove = new TaskCompletionSource<bool>(
						TaskCreationOptions.RunContinuationsAsynchronously)
		};

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var readStarted = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var allowRead = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

		var feedReader = new FakeFeedReader
		{
			ReadHandler = async (url, token) =>
			{
				readStarted.TrySetResult(true);

				await allowRead.Task.WaitAsync(token);

				return new List<FeedItem>();
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		DateTimeOffset? originalLastRetrieved =
				subscription.LastRetrieved;

		Task refreshTask =
				viewModel.RefreshSubscriptionInBackgroundAsync(subscription);

		Task<bool>? removalTask = null;

		try
		{
			// Wait until the background refresh is retrieving the feed.
			await readStarted.Task.WaitAsync(
					TimeSpan.FromSeconds(5));

			// Start removal, but keep the repository operation pending.
			removalTask = viewModel.RemoveSubscriptionAsync(subscription);

			await subscriptionRepository.RemoveStarted.Task.WaitAsync(
					TimeSpan.FromSeconds(5));

			Assert.True(viewModel.IsRemovingSubscription);
			Assert.Contains(subscription, viewModel.Subscriptions);

			// Complete retrieval while removal is still pending.
			allowRead.TrySetResult(true);

			await refreshTask;

			// The subscription still exists, but is being removed.
			Assert.Contains(subscription, viewModel.Subscriptions);

			// Refresh must not update its displayed state.
			Assert.Equal(
					originalLastRetrieved,
					subscription.LastRetrieved);

			Assert.Null(subscription.RefreshStatus);
			Assert.Null(subscription.RefreshError);
			Assert.False(subscription.IsRefreshing);

			// It must not attempt to update the database timestamp.
			Assert.Equal(
					0,
					subscriptionRepository.UpdateLastRetrievedCount);
		}
		finally
		{
			allowRead.TrySetResult(true);
			subscriptionRepository.AllowRemove.TrySetResult(true);

			await refreshTask;

			if (removalTask is not null)
			{
				await removalTask;
			}
		}

		Assert.Empty(viewModel.Subscriptions);
		Assert.False(viewModel.IsRemovingSubscription);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_DoesNotReportSuccessWhenTimestampUpdateFails()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		var feedReader = new FakeFeedReader
		{
			ReadHandler = (url, token) =>
			{
				// Simulate the subscription disappearing from
				// persistence while the refresh is underway.
				subscriptionRepository.Subscriptions.Clear();

				return Task.FromResult(new List<FeedItem>());
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		await viewModel.RefreshSubscriptionInBackgroundAsync(
				subscription);

		Assert.Equal(
				1,
				subscriptionRepository.UpdateLastRetrievedCount);

		Assert.Null(subscription.RefreshStatus);
		Assert.Null(subscription.RefreshError);
		Assert.False(subscription.IsRefreshing);
		Assert.Null(subscription.LastRetrieved);
	}
	[Fact]
	public async Task RefreshSubscriptionInBackgroundAsync_AllowsRetryAfterFailure()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		await subscriptionService.AddAsync(new FeedSubscription
		{
			Url = feedUrl,
			Title = "Test Feed"
		});

		int readCount = 0;

		var feedReader = new FakeFeedReader
		{
			ReadHandler = (url, token) =>
			{
				readCount++;

				if (readCount == 1)
				{
					throw new HttpRequestException(
							"Simulated temporary network failure.");
				}

				return Task.FromResult(new List<FeedItem>());
			}
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());


		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		await viewModel.LoadSubscriptionsAsync();

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		// First attempt fails.
		await viewModel.RefreshSubscriptionInBackgroundAsync(
				subscription);

		Assert.Equal(1, readCount);
		Assert.False(subscription.IsRefreshing);
		Assert.NotNull(subscription.RefreshError);
		Assert.Null(subscription.RefreshStatus);

		// Second attempt succeeds.
		await viewModel.RefreshSubscriptionInBackgroundAsync(
				subscription);

		Assert.Equal(2, readCount);
		Assert.False(subscription.IsRefreshing);
		Assert.Null(subscription.RefreshError);

		Assert.Equal(
				"Refreshed successfully.",
				subscription.RefreshStatus);
	}
	[Fact]
	public async Task AddSubscriptionAsync_BlankTitle_UsesDiscoveredFeedTitle()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			TitleToReturn = "Example News"
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());



		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		AddSubscriptionResult result =
				await viewModel.AddSubscriptionAsync(feedUrl, "");

		Assert.Equal(AddSubscriptionResult.Success, result);

		Assert.Equal(1, feedReader.TitleReadCount);

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		Assert.Equal("Example News", subscription.Title);

		FeedSubscription storedSubscription =
				Assert.Single(subscriptionRepository.Subscriptions);

		Assert.Equal("Example News", storedSubscription.Title);
	}
	[Fact]
	public async Task AddSubscriptionAsync_CustomTitle_SkipsTitleDiscovery()
	{
		const string feedUrl = "https://example.com/feed.xml";
		const string customTitle = "My Favorite News";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			TitleToReturn = "Original Feed Title"
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());



		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var result =
				await viewModel.AddSubscriptionAsync(
						feedUrl,
						customTitle);

		Assert.Equal("Success", result.ToString());

		// A custom title should eliminate the metadata request.
		Assert.Equal(0, feedReader.TitleReadCount);

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		Assert.Equal(customTitle, subscription.Title);

		FeedSubscription storedSubscription =
				Assert.Single(subscriptionRepository.Subscriptions);

		Assert.Equal(customTitle, storedSubscription.Title);
	}
	[Fact]
	public async Task AddSubscriptionAsync_TitleDiscoveryFails_UsesUrlFallback()
	{
		const string feedUrl = "https://example.com/feed.xml";

		var subscriptionRepository = new FakeSubscriptionRepository();

		var subscriptionService =
				new SubscriptionService(subscriptionRepository);

		var feedReader = new FakeFeedReader
		{
			TitleException = new HttpRequestException(
						"Simulated network failure.")
		};

		var feedService = new FeedService(
				subscriptionService,
				feedReader,
				new FakeArticleRepository());

		var articleStateService = new ArticleStateService(
				new FakeArticleStateRepository());



		var viewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

		var result =
				await viewModel.AddSubscriptionAsync(feedUrl, "");

		Assert.Equal("Success", result.ToString());
		Assert.Equal(1, feedReader.TitleReadCount);

		SubscriptionViewModel subscription =
				Assert.Single(viewModel.Subscriptions);

		Assert.Equal(feedUrl, subscription.Title);

		FeedSubscription storedSubscription =
				Assert.Single(subscriptionRepository.Subscriptions);

		Assert.Equal(feedUrl, storedSubscription.Title);
	}
	[Fact]
	public async Task PrepareOpmlImportAsync_DoesNotSaveSubscriptions()
	{
		var (viewModel, repository) = CreateViewModel();

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="New Feed"
                     xmlUrl="https://example.com/new.xml" />
          </body>
        </opml>
        """;

		List<OpmlImportEntry> entries =
						await viewModel.PrepareOpmlImportAsync(xml);

		OpmlImportEntry entry = Assert.Single(entries);

		Assert.Equal(OpmlImportStatus.New, entry.Status);
		Assert.Equal(
						"https://example.com/new.xml",
						entry.Subscription.Url);

		Assert.Empty(repository.Subscriptions);
		Assert.Empty(viewModel.Subscriptions);
	}
	[Fact]
	public async Task PrepareOpmlImportAsync_ClassifiesMixedEntries()
	{
		var (viewModel, repository) = CreateViewModel();

		repository.Subscriptions.Add(new FeedSubscription
		{
			Url = "https://example.com/existing.xml",
			Title = "Existing Feed"
		});

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="New Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Existing Feed"
                     xmlUrl="https://example.com/existing.xml" />
            <outline text="Duplicate Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Invalid Feed"
                     xmlUrl="not-a-url" />
          </body>
        </opml>
        """;

		List<OpmlImportEntry> entries =
						await viewModel.PrepareOpmlImportAsync(xml);

		Assert.Equal(4, entries.Count);

		Assert.Equal(OpmlImportStatus.New, entries[0].Status);
		Assert.Equal(OpmlImportStatus.AlreadySubscribed, entries[1].Status);
		Assert.Equal(OpmlImportStatus.DuplicateInFile, entries[2].Status);
		Assert.Equal(OpmlImportStatus.InvalidUrl, entries[3].Status);

		Assert.Single(repository.Subscriptions);
		Assert.Empty(viewModel.Subscriptions);
	}
	[Fact]
	public async Task ExecuteOpmlImportAsync_SavesOnlyNewSubscriptions()
	{
		var (viewModel, repository) = CreateViewModel();

		repository.Subscriptions.Add(new FeedSubscription
		{
			Url = "https://example.com/existing.xml",
			Title = "Existing Feed"
		});

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="New Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Existing Feed"
                     xmlUrl="https://example.com/existing.xml" />
            <outline text="Duplicate Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Invalid Feed"
                     xmlUrl="not-a-url" />
          </body>
        </opml>
        """;

		List<OpmlImportEntry> entries =
						await viewModel.PrepareOpmlImportAsync(xml);

		Assert.Single(repository.Subscriptions);

		List<OpmlImportExecutionResult> results =
						await viewModel.ExecuteOpmlImportAsync(entries);

		Assert.Equal(4, results.Count);

		Assert.Equal(OpmlImportExecutionStatus.Added, results[0].Status);
		Assert.Equal(OpmlImportExecutionStatus.Skipped, results[1].Status);
		Assert.Equal(OpmlImportExecutionStatus.Skipped, results[2].Status);
		Assert.Equal(OpmlImportExecutionStatus.Skipped, results[3].Status);

		Assert.Equal(2, repository.Subscriptions.Count);
		Assert.Equal(2, viewModel.Subscriptions.Count);
	}
	[Fact]
	public async Task ExecuteOpmlImportAsync_WhenSaveFails_ReportsPartialImport()
	{
		var (viewModel, repository) = CreateViewModel();

		repository.FailOnAddNumber = 2;

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="First Feed"
                     xmlUrl="https://example.com/first.xml" />
            <outline text="Second Feed"
                     xmlUrl="https://example.com/second.xml" />
            <outline text="Third Feed"
                     xmlUrl="https://example.com/third.xml" />
          </body>
        </opml>
        """;

		List<OpmlImportEntry> entries =
						await viewModel.PrepareOpmlImportAsync(xml);

		List<OpmlImportExecutionResult> results =
						await viewModel.ExecuteOpmlImportAsync(entries);

		Assert.Equal(3, results.Count);

		Assert.Equal(OpmlImportExecutionStatus.Added, results[0].Status);
		Assert.Equal(OpmlImportExecutionStatus.Failed, results[1].Status);
		Assert.Equal(OpmlImportExecutionStatus.Added, results[2].Status);

		Assert.Equal(
						"Imported 2 subscriptions. Failed: 1.",
						viewModel.OpmlImportStatus);

		Assert.Equal(2, repository.Subscriptions.Count);
		Assert.Equal(2, viewModel.Subscriptions.Count);
	}
	[Fact]
	public async Task ExecuteOpmlImportAsync_ReportsSkippedEntriesByReason()
	{
		var (viewModel, repository) = CreateViewModel();

		repository.Subscriptions.Add(new FeedSubscription
		{
			Url = "https://example.com/existing.xml",
			Title = "Existing Feed"
		});

		const string xml = """
        <opml version="2.0">
          <body>
            <outline text="New Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Existing Feed"
                     xmlUrl="https://example.com/existing.xml" />
            <outline text="Duplicate Feed"
                     xmlUrl="https://example.com/new.xml" />
            <outline text="Invalid Feed"
                     xmlUrl="not-a-url" />
          </body>
        </opml>
        """;

		List<OpmlImportEntry> entries =
						await viewModel.PrepareOpmlImportAsync(xml);

		await viewModel.ExecuteOpmlImportAsync(entries);

		Assert.Equal(
						"Imported 1 subscription. Already subscribed: 1. Duplicates in file: 1. Invalid URLs: 1.",
						viewModel.OpmlImportStatus);

		Assert.Equal(2, repository.Subscriptions.Count);
	}

}
