// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RssReader.App.Results;
using RssReader.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace RssReader.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	[ObservableProperty]
	public partial SubscriptionViewModel? SelectedSubscription { get; set; }

	[ObservableProperty]
	public partial string? ArticleLoadError { get; set; }

	[ObservableProperty]
	public partial bool IsLoadingArticles { get; set; }

	[ObservableProperty]
	public partial bool HasSelectedSubscription { get; set; }

	[ObservableProperty]
	public partial bool HasNoSelectedSubscription { get; set; } = true;

	[ObservableProperty]
	public partial bool HasNoArticles { get; set; }

	[ObservableProperty]
	public partial ArticleViewModel? SelectedArticle { get; set; }

	[ObservableProperty]
	public partial bool HasSelectedArticle { get; set; }

	[ObservableProperty]
	public partial bool HasNoSelectedArticle { get; set; } = true;

	[ObservableProperty]
	public partial bool IsRefreshingArticles { get; set; }

	[ObservableProperty]
	public partial bool IsRemovingSubscription { get; set; }

	[ObservableProperty]
	public partial bool IsRenamingSubscription { get; set; }

	[ObservableProperty]
	public partial string? SubscriptionRenameError { get; set; }

	[ObservableProperty]
	public partial string? SubscriptionOperationError { get; set; }

	[ObservableProperty]
	public partial string? ArticleStateError { get; set; }

	[ObservableProperty]
	public partial bool HasEmptyFeed { get; set; }

	[ObservableProperty]
	public partial bool IsImportingOpml { get; set; }

	[ObservableProperty]
	public partial string? OpmlImportStatus { get; set; }

	[ObservableProperty]
	public partial string? OpmlImportError { get; set; }

	[ObservableProperty]
	public partial bool IsExportingOpml { get; set; }

	[ObservableProperty]
	public partial string? OpmlExportStatus { get; set; }

	[ObservableProperty]
	public partial string? OpmlExportError { get; set; }

	private readonly SubscriptionService _subscriptionService;

	private readonly OpmlService _opmlService;

	private readonly FeedService _feedService;

	private readonly ArticleStateService _articleStateService;

	private readonly SubscriptionImportService _subscriptionImportService;

	private readonly HashSet<string> _backgroundRefreshes =
				new(StringComparer.OrdinalIgnoreCase);

	private int _articleLoadRequestId;

	private CancellationTokenSource? _articleLoadCancellation;

	private SubscriptionViewModel? _subscriptionBeingRemoved;

	private CancellationTokenSource BeginArticleLoad()
	{
		_articleLoadCancellation?.Cancel();

		_articleLoadCancellation = new CancellationTokenSource();

		return _articleLoadCancellation;
	}

	private void CancelArticleLoad()
	{
		_articleLoadCancellation?.Cancel();
	}

	internal Task? InitialArticleLoadTask { get; private set; }

	public ObservableCollection<SubscriptionViewModel> Subscriptions { get; } = new();

	public ObservableCollection<ArticleViewModel> Articles { get; } = new();

	public bool CanMarkSelectedArticleAsRead => SelectedArticle is not null && !SelectedArticle.IsRead;

	public bool CanMarkSelectedArticleAsUnread => SelectedArticle is not null && SelectedArticle.IsRead;

	public bool CanRemoveSubscription => HasSelectedSubscription && !IsRemovingSubscription;

	public bool CanRefreshSelectedFeed =>
			HasSelectedSubscription
			&& !IsLoadingArticles
			&& !IsRefreshingArticles;

	public bool HasArticlesAwaitingSelection => HasSelectedSubscription && HasNoSelectedArticle && !HasNoArticles && !IsLoadingArticles && ArticleLoadError is null;

	partial void OnHasSelectedSubscriptionChanged(bool value)
	{
		OnPropertyChanged(nameof(CanRemoveSubscription));
		OnPropertyChanged(nameof(HasArticlesAwaitingSelection));
		OnPropertyChanged(nameof(CanRefreshSelectedFeed));
	}

	partial void OnIsRemovingSubscriptionChanged(bool value)
	{
		OnPropertyChanged(nameof(CanRemoveSubscription));
	}

	partial void OnIsRefreshingArticlesChanged(bool value)
	{
		OnPropertyChanged(nameof(CanRefreshSelectedFeed));
	}

	partial void OnHasNoSelectedArticleChanged(bool value)
	{
		OnPropertyChanged(nameof(HasArticlesAwaitingSelection));
	}

	partial void OnHasNoArticlesChanged(bool value)
	{
		OnPropertyChanged(nameof(HasArticlesAwaitingSelection));
	}

	partial void OnIsLoadingArticlesChanged(bool value)
	{
		OnPropertyChanged(nameof(HasArticlesAwaitingSelection));
	}

	partial void OnArticleLoadErrorChanged(string? value)
	{
		OnPropertyChanged(nameof(HasArticlesAwaitingSelection));
	}

	private void RefreshSelectedArticleReadState()
	{
		OnPropertyChanged(nameof(CanMarkSelectedArticleAsRead));
		OnPropertyChanged(nameof(CanMarkSelectedArticleAsUnread));
	}

	public MainViewModel(SubscriptionService subscriptionService, FeedService feedService, ArticleStateService articleStateService, SubscriptionImportService subscriptionImportService, OpmlService opmlService)
	{
		_subscriptionService = subscriptionService;
		_feedService = feedService;
		_articleStateService = articleStateService;
		_subscriptionImportService = subscriptionImportService;
		_opmlService = opmlService;
	}
	public async Task LoadSubscriptionsAsync()
	{
		var subscriptions = await _subscriptionService.GetAllAsync();

		foreach (var subscription in subscriptions)
		{
			bool alreadyDisplayed = Subscriptions.Any(existing =>
					string.Equals(
							existing.Item.Url,
							subscription.Url,
							StringComparison.OrdinalIgnoreCase));

			if (!alreadyDisplayed)
			{
				Subscriptions.Add(
						new SubscriptionViewModel(subscription));
			}
		}
	}


	public async Task<AddSubscriptionResult> AddSubscriptionAsync(string url,string title)
	{
		try
		{
			string subscriptionTitle = title;

			if (string.IsNullOrWhiteSpace(subscriptionTitle))
			{
				subscriptionTitle = url;

				try
				{
					string? discoveredTitle =
									await _feedService.GetFeedTitleAsync(url);

					if (!string.IsNullOrWhiteSpace(discoveredTitle))
					{
						subscriptionTitle = discoveredTitle;
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(
									$"Unable to discover feed title for {url}: {ex}");
				}
			}

			var subscription = new FeedSubscription
			{
				Url = url,
				Title = subscriptionTitle
			};

			bool added = await _subscriptionService.AddAsync(subscription);

			if (!added)
			{
				return AddSubscriptionResult.AlreadyExists;
			}

			var subscriptionViewModel =
							new SubscriptionViewModel(subscription);

			Subscriptions.Add(subscriptionViewModel);
			SelectedSubscription = subscriptionViewModel;

			return AddSubscriptionResult.Success;
		}
		catch (ArgumentException)
		{
			return AddSubscriptionResult.InvalidUrl;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
							$"Failed to add subscription {url}: {ex}");

			return AddSubscriptionResult.Failed;
		}
	}



	partial void OnSelectedSubscriptionChanged(SubscriptionViewModel? value)
	{
		HasSelectedSubscription = value is not null;
		HasNoSelectedSubscription = value is null;

		if (value is not null)
		{
			InitialArticleLoadTask = LoadArticlesSafelyAsync(value.Item);
		}
		else
		{
			CancelArticleLoad();

			++_articleLoadRequestId;
			Articles.Clear();
			ArticleLoadError = null;
			IsLoadingArticles = false;
			HasNoArticles = false;
			IsRefreshingArticles = false;
		}
	}

	//introduce a cache-loading helper
	private async Task<List<ArticleViewModel>> GetStoredArticleViewModelsAsync(string feedUrl)
	{
		List<FeedItem> storedItems =
				await _feedService.GetStoredItemsAsync(feedUrl);

		var articles = new List<ArticleViewModel>();

		foreach (FeedItem item in storedItems)
		{
			bool isRead = await _articleStateService.IsReadAsync(
					feedUrl,
					item);

			articles.Add(new ArticleViewModel(item, isRead));
		}

		return articles;
	}


	private async Task LoadArticlesSafelyAsync(FeedSubscription subscription, bool isRefresh = false)
	{
		CancellationTokenSource cancellationSource = BeginArticleLoad();
		CancellationToken cancellationToken = cancellationSource.Token;

		int requestId = ++_articleLoadRequestId;

		string? selectedArticleId = isRefresh
						? SelectedArticle?.Item.Id
						: null;

		string? selectedArticleLink = isRefresh
						? SelectedArticle?.Item.Link
						: null;

		IsLoadingArticles = !isRefresh;
		IsRefreshingArticles = isRefresh;

		if (!isRefresh)
		{
			Articles.Clear();
			SelectedArticle = null;
			HasNoArticles = false;
		}

		ArticleLoadError = null;

		try
		{
			if (!isRefresh)
			{
				try
				{
					List<ArticleViewModel> storedArticles =
									await GetStoredArticleViewModelsAsync(subscription.Url);

					if (requestId != _articleLoadRequestId)
					{
						return;
					}

					foreach (ArticleViewModel article in storedArticles)
					{
						Articles.Add(article);
					}

					HasNoArticles = Articles.Count == 0;

					SubscriptionViewModel? selectedSubscription =
									Subscriptions.FirstOrDefault(
													s => s.Item.Url == subscription.Url);

					if (selectedSubscription is not null)
					{
						selectedSubscription.UnreadCount =
										storedArticles.Count(article => !article.IsRead);
					}
				}
				catch (ArticleStorageException ex)
				{
					if (requestId != _articleLoadRequestId)
					{
						return;
					}

					Console.Error.WriteLine(
									$"Unable to read cached articles from {subscription.Url}: {ex}");
				}
			}

			List<FeedItem> retrievedArticles =
							await _feedService.GetItemsAsync(
											subscription.Url,
											cancellationToken);

			if (requestId != _articleLoadRequestId)
			{
				return;
			}

			List<FeedItem> articles;

			try
			{
				articles = await _feedService.GetStoredItemsAsync(subscription.Url);
			}
			catch (ArticleStorageException ex)
							when (ex.Operation == ArticleStorageOperation.Read && !isRefresh)
			{
				if (requestId != _articleLoadRequestId)
				{
					return;
				}

				Console.Error.WriteLine(
								$"Unable to read saved articles from {subscription.Url}: {ex}");

				articles = retrievedArticles;

				ArticleLoadError =
								"Unable to read saved articles from local storage. Showing recently retrieved articles instead.";
			}

			if (requestId != _articleLoadRequestId)
			{
				return;
			}

			var loadedArticles = new List<ArticleViewModel>();
			int unreadCount = 0;

			foreach (var article in articles)
			{
				bool isRead = await _articleStateService.IsReadAsync(
								subscription.Url,
								article);

				if (requestId != _articleLoadRequestId)
				{
					return;
				}

				if (!isRead)
				{
					unreadCount++;
				}

				loadedArticles.Add(new ArticleViewModel(article, isRead));
			}

			if (requestId != _articleLoadRequestId)
			{
				return;
			}

			Articles.Clear();

			foreach (var article in loadedArticles)
			{
				Articles.Add(article);
			}

			SelectedArticle = loadedArticles.FirstOrDefault(
							article =>
											(!string.IsNullOrEmpty(selectedArticleId)
											&& article.Item.Id == selectedArticleId)
											|| (!string.IsNullOrEmpty(selectedArticleLink)
											&& article.Item.Link == selectedArticleLink));

			SubscriptionViewModel? subscriptionViewModel =
							Subscriptions.FirstOrDefault(
											s => s.Item.Url == subscription.Url);

			if (subscriptionViewModel is not null)
			{
				subscriptionViewModel.UnreadCount = unreadCount;
			}

			HasNoArticles = Articles.Count == 0;

			if (ArticleLoadError is null)
			{
				DateTimeOffset lastRetrieved = DateTimeOffset.UtcNow;

				bool timestampUpdated =
								await _subscriptionService.UpdateLastRetrievedAsync(
												subscription.Url,
												lastRetrieved);

				if (timestampUpdated)
				{
					if (subscriptionViewModel is not null)
					{
						subscriptionViewModel.LastRetrieved = lastRetrieved;
					}
				}
				else
				{
					Console.Error.WriteLine(
									$"Unable to update retrieval timestamp for {subscription.Url}");
				}
			}
		}
		catch (OperationCanceledException)
						when (cancellationToken.IsCancellationRequested)
		{
			// An obsolete request was canceled intentionally.
		}
		catch (ArticleStorageException ex)
		{
			if (requestId != _articleLoadRequestId)
			{
				return;
			}

			Console.Error.WriteLine(
							$"Failed to save articles from {subscription.Url}: {ex}");

			ArticleLoadError = ex.Operation switch
			{
				ArticleStorageOperation.Read =>
								"Unable to read saved articles from local storage.",

				ArticleStorageOperation.Write =>
								"Unable to save retrieved articles. Your previously saved articles are still available.",

				_ =>
								"Unable to access local article storage."
			};
		}
		catch (Exception ex)
		{
			if (requestId != _articleLoadRequestId)
			{
				return;
			}

			Console.Error.WriteLine(
							$"Failed to load articles from {subscription.Url}: {ex}");

			ArticleLoadError =
							"Unable to load articles. Check your connection and try again.";
		}
		finally
		{
			if (requestId == _articleLoadRequestId)
			{
				IsLoadingArticles = false;
				IsRefreshingArticles = false;
			}

			if (ReferenceEquals(
							_articleLoadCancellation,
							cancellationSource))
			{
				_articleLoadCancellation = null;
			}

			cancellationSource.Dispose();
		}
	}


	partial void OnSelectedArticleChanged(ArticleViewModel? value)
	{
		HasSelectedArticle = value is not null;
		HasNoSelectedArticle = value is null;
		RefreshSelectedArticleReadState();
	}

	//failed removal sim is in the following, for future reference 

	public async Task<bool> RemoveSubscriptionAsync(SubscriptionViewModel subscription)
	{
		if (IsRemovingSubscription)
		{
			return false;
		}

		IsRemovingSubscription = true;
		SubscriptionOperationError = null;
		_subscriptionBeingRemoved = subscription;


		try
		{

			/***************************************************************
			* For simulating a failed removal; comment out when not needed *
			***************************************************************/

			//throw new InvalidOperationException("Simulated database failure");

			bool removed = await _subscriptionService.RemoveAsync(
					subscription.Item.Url);

			if (!removed)
			{
				SubscriptionOperationError =
						"The subscription could not be found.";

				return false;
			}

			if (SelectedSubscription == subscription)
			{
				SelectedSubscription = null;
			}

			Subscriptions.Remove(subscription);

			return true;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
					$"Failed to remove subscription {subscription.Item.Url}: {ex}");

			SubscriptionOperationError =
					"Unable to remove the subscription. Please try again.";

			return false;
		}
		finally
		{
			IsRemovingSubscription = false;
			_subscriptionBeingRemoved = null;
		}
	}

	public async Task<bool> RenameSubscriptionAsync(SubscriptionViewModel subscription, string newTitle)
	{
		if (IsRenamingSubscription)
		{
			return false;
		}

		IsRenamingSubscription = true;
		SubscriptionRenameError = null;

		try
		{
			bool renamed = await _subscriptionService.RenameAsync(
					subscription.Item.Url,
					newTitle);

			if (!renamed)
			{
				SubscriptionRenameError =
						"The subscription could not be found.";

				return false;
			}

			subscription.Title = newTitle.Trim();

			return true;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
					$"Failed to rename subscription {subscription.Item.Url}: {ex}");

			SubscriptionRenameError =
					"Unable to rename the subscription. Please try again.";

			return false;
		}
		finally
		{
			IsRenamingSubscription = false;
		}
	}
	[RelayCommand]
	private Task MarkSelectedArticleAsReadAsync()
	{
		return SetSelectedArticleReadStateAsync(true);
	}

	[RelayCommand]
	private Task MarkSelectedArticleAsUnreadAsync()
	{
		return SetSelectedArticleReadStateAsync(false);
	}

	[RelayCommand]
	private Task RefreshSelectedFeedAsync()
	{
		if (SelectedSubscription is null
				|| IsLoadingArticles
				|| IsRefreshingArticles)
		{
			return Task.CompletedTask;
		}

		return LoadArticlesSafelyAsync(
				SelectedSubscription.Item,
				isRefresh: true);
	}


	public async Task RefreshSubscriptionInBackgroundAsync(SubscriptionViewModel subscription)
	{
		string feedUrl = subscription.Item.Url;

		if (!_backgroundRefreshes.Add(feedUrl))
		{
			return;
		}

		subscription.IsRefreshing = true;
		subscription.RefreshStatus = null;
		subscription.RefreshError = null;

		try
		{
			await _feedService.GetItemsAsync(feedUrl);

			if (!Subscriptions.Contains(subscription)
							|| ReferenceEquals(_subscriptionBeingRemoved, subscription))
			{
				return;
			}

			List<ArticleViewModel> storedArticles =
				await GetStoredArticleViewModelsAsync(feedUrl);

			if (!Subscriptions.Contains(subscription)
				|| ReferenceEquals(_subscriptionBeingRemoved, subscription))
			{
				return;
			}

			subscription.UnreadCount =
							storedArticles.Count(article => article.IsUnread);

			DateTimeOffset lastRetrieved = DateTimeOffset.UtcNow;

			bool updated =
							await _subscriptionService.UpdateLastRetrievedAsync(
											feedUrl,
											lastRetrieved);

			if (!updated)
			{
				return;
			}

			if (!Subscriptions.Contains(subscription)
							|| ReferenceEquals(_subscriptionBeingRemoved, subscription))
			{
				return;
			}

			subscription.LastRetrieved = lastRetrieved;

			subscription.RefreshStatus =
							"Refreshed successfully.";
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
							$"Background refresh failed for {feedUrl}: {ex}");

			subscription.RefreshError =
							"Unable to refresh feed. Check your connection and try again.";
		}
		finally
		{
			subscription.IsRefreshing = false;
			_backgroundRefreshes.Remove(feedUrl);
		}
	}


	private async Task SetSelectedArticleReadStateAsync(bool isRead)
	{
		ArticleViewModel? article = SelectedArticle;
		SubscriptionViewModel? subscription = SelectedSubscription;

		if (article is null || subscription is null)
			return;

		ArticleStateError = null;

		try
		{
			if (isRead)
			{
				await _articleStateService.MarkAsReadAsync(
						subscription.Item.Url,
						article.Item);
			}
			else
			{
				await _articleStateService.MarkAsUnreadAsync(
						subscription.Item.Url,
						article.Item);
			}

			bool wasRead = article.IsRead;

			article.IsRead = isRead;
			RefreshSelectedArticleReadState();

			if (subscription.UnreadCount.HasValue && wasRead != isRead)
			{
				subscription.UnreadCount += isRead ? -1 : 1;
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
					$"Failed to update article read state: {ex}");

			ArticleStateError =
					"Unable to update the article's read status. Please try again.";
		}
	}

	[RelayCommand]
	private void ClearArticleSelection()
	{
		SelectedArticle = null;
	}

	[RelayCommand]
	private void ClearSubscriptionSelection()
	{
		SelectedSubscription = null;
	}

	[RelayCommand]
	private async Task ImportOpmlAsync(string xml)
	{
		IsImportingOpml = true;
		OpmlImportStatus = null;
		OpmlImportError = null;

		try
		{
			SubscriptionImportResult result =
					await _subscriptionImportService.ImportAsync(xml);

			await LoadSubscriptionsAsync();

			string subscriptionWord =
					result.Added.Count == 1 ? "subscription" : "subscriptions";

			OpmlImportStatus =
					$"Imported {result.Added.Count} {subscriptionWord}.";

			if (result.AlreadySubscribed.Count > 0)
			{
				OpmlImportStatus +=
						$" Already subscribed: {result.AlreadySubscribed.Count}.";
			}

			if (result.Issues.Count > 0)
			{
				OpmlImportStatus +=
						$" Invalid entries: {result.Issues.Count}.";
			}

			if (result.Failures.Count > 0)
			{
				OpmlImportStatus +=
						$" Failed to save: {result.Failures.Count}.";
			}

			if (result.NotAttempted.Count > 0)
			{
				OpmlImportStatus +=
						$" Not attempted: {result.NotAttempted.Count}.";
			}
		}
		catch (XmlException)
		{
			OpmlImportError =
					"Unable to import OPML: The file contains malformed XML.";
		}
		catch (FormatException)
		{
			OpmlImportError =
					"Unable to import OPML: The file is not a valid OPML document.";
		}
		finally
		{
			IsImportingOpml = false;
		}
	}

	public async Task<string> ExportOpmlAsync()
	{
		List<FeedSubscription> subscriptions =
				await _subscriptionService.GetAllAsync();

		return _opmlService.Export(subscriptions);
	}
}
