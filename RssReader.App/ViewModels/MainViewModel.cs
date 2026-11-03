using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RssReader.App.Results;
using RssReader.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
	public partial string? SubscriptionOperationError { get; set; }

	[ObservableProperty]
	public partial string? ArticleStateError { get; set; }

	[ObservableProperty]
	public partial bool HasEmptyFeed { get; set; }

	private readonly SubscriptionService _subscriptionService;

	private readonly FeedService _feedService;

	private readonly ArticleStateService _articleStateService;

	private int _articleLoadRequestId;

	public ObservableCollection<SubscriptionViewModel> Subscriptions { get; } = new();

	public ObservableCollection<ArticleViewModel> Articles { get; } = new();

	public bool CanMarkSelectedArticleAsRead => SelectedArticle is not null && !SelectedArticle.IsRead;

	public bool CanMarkSelectedArticleAsUnread => SelectedArticle is not null && SelectedArticle.IsRead;

	public bool CanRemoveSubscription => HasSelectedSubscription && !IsRemovingSubscription;

	public bool HasArticlesAwaitingSelection => HasSelectedSubscription && HasNoSelectedArticle && !HasNoArticles && !IsLoadingArticles && ArticleLoadError is null;

	partial void OnHasSelectedSubscriptionChanged(bool value)
	{
		OnPropertyChanged(nameof(CanRemoveSubscription));
		OnPropertyChanged(nameof(HasArticlesAwaitingSelection));
	}

	partial void OnIsRemovingSubscriptionChanged(bool value)
	{
		OnPropertyChanged(nameof(CanRemoveSubscription));
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

	public MainViewModel(SubscriptionService subscriptionService, FeedService feedService, ArticleStateService articleStateService)
	{
		_subscriptionService = subscriptionService;
		_feedService = feedService;
		_articleStateService = articleStateService;
	}
	public async Task LoadSubscriptionsAsync()
	{
		var subscriptions = await _subscriptionService.GetAllAsync();

		Subscriptions.Clear();

		foreach (var subscription in subscriptions)
		{
			Subscriptions.Add(new SubscriptionViewModel(subscription));
		}
	}

	public async Task<AddSubscriptionResult> AddSubscriptionAsync(string url, string title)
	{
		var subscription = new FeedSubscription
		{
			Url = url,
			Title = string.IsNullOrWhiteSpace(title) ? url : title
		};

		try
		{
			bool added = await _subscriptionService.AddAsync(subscription);

			if (!added)
			{
				return AddSubscriptionResult.AlreadyExists;
			}

			var subscriptionViewModel = new SubscriptionViewModel(subscription);

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
			_ = LoadArticlesSafelyAsync(value.Item);
		}
		else
		{
			++_articleLoadRequestId;
			Articles.Clear();
			ArticleLoadError = null;
			IsLoadingArticles = false;
			HasNoArticles = false;
			IsRefreshingArticles = false;
		}
	}

	private async Task LoadArticlesSafelyAsync(FeedSubscription subscription, bool isRefresh = false)
	{
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
			var articles = await _feedService.GetItemsAsync(
							subscription.Url);

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
								!string.IsNullOrEmpty(selectedArticleId)
								&& article.Item.Id == selectedArticleId
								|| !string.IsNullOrEmpty(selectedArticleLink)
								&& article.Item.Link == selectedArticleLink);

			SubscriptionViewModel? subscriptionViewModel =
							Subscriptions.FirstOrDefault(
											s => s.Item.Url == subscription.Url);

			if (subscriptionViewModel is not null)
			{
				subscriptionViewModel.UnreadCount = unreadCount;
			}

			HasNoArticles = Articles.Count == 0;
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
		if (SelectedSubscription is null)
		{
			return Task.CompletedTask;
		}

		return LoadArticlesSafelyAsync(
				SelectedSubscription.Item,
				isRefresh: true);
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
}
