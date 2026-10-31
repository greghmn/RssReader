using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using RssReader.App.Results;
using CommunityToolkit.Mvvm.Input;

namespace RssReader.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial FeedSubscription? SelectedSubscription { get; set; }
    
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

    private readonly SubscriptionService _subscriptionService;

    private readonly FeedService _feedService;

    private readonly ArticleStateService _articleStateService;

    private int _articleLoadRequestId;

    public ObservableCollection<FeedSubscription> Subscriptions { get; } = new();

    public ObservableCollection<ArticleViewModel> Articles { get; } = new();

    public bool CanMarkSelectedArticleAsRead =>
    SelectedArticle is not null && !SelectedArticle.IsRead;

    public bool CanMarkSelectedArticleAsUnread =>
        SelectedArticle is not null && SelectedArticle.IsRead;

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
            Subscriptions.Add(subscription);
        }
    }

    public async Task<AddSubscriptionResult> AddSubscriptionAsync(
        string url,
        string title)
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

            Subscriptions.Add(subscription);
            SelectedSubscription = subscription;

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


    partial void OnSelectedSubscriptionChanged(FeedSubscription? value)
    {
        HasSelectedSubscription = value is not null;
        HasNoSelectedSubscription = value is null;
        if (value is not null)
        {
            _ = LoadArticlesSafelyAsync(value);
        }
        else
        {
            ++_articleLoadRequestId;
            Articles.Clear();
            ArticleLoadError = null;
            IsLoadingArticles = false;
            HasNoArticles = false;
        }
    }

    private async Task LoadArticlesSafelyAsync(FeedSubscription subscription)
    {
        int requestId = ++_articleLoadRequestId;
        IsLoadingArticles = true;
        HasNoArticles = false;
        Articles.Clear();
        ArticleLoadError = null;

        try
        {
            var articles = await _feedService.GetItemsAsync(
                subscription.Url);

            if (requestId != _articleLoadRequestId)
            {
                return;
            }

            foreach (var article in articles)
            {
                bool isRead = await _articleStateService.IsReadAsync(
                    subscription.Url,
                    article);

                if (requestId != _articleLoadRequestId)
                {
                    return;
                }

                Articles.Add(new ArticleViewModel(article, isRead));
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
            }
        }
    }
    partial void OnSelectedArticleChanged(ArticleViewModel? value)
    {
        HasSelectedArticle = value is not null;
        HasNoSelectedArticle = value is null;
        RefreshSelectedArticleReadState();
    }

    public async Task<bool> RemoveSubscriptionAsync(
    FeedSubscription subscription)
    {
        bool removed = await _subscriptionService.RemoveAsync(
            subscription.Url);

        if (!removed)
        {
            return false;
        }

        if (SelectedSubscription == subscription)
        {
            SelectedSubscription = null;
        }

        Subscriptions.Remove(subscription);

        return true;
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

    private async Task SetSelectedArticleReadStateAsync(bool isRead)
    {
        ArticleViewModel? article = SelectedArticle;
        FeedSubscription? subscription = SelectedSubscription;

        if (article is null || subscription is null)
            return;

        try
        {
            if (isRead)
            {
                await _articleStateService.MarkAsReadAsync(
                    subscription.Url,
                    article.Item);
            }
            else
            {
                await _articleStateService.MarkAsUnreadAsync(
                    subscription.Url,
                    article.Item);
            }

            article.IsRead = isRead;
            RefreshSelectedArticleReadState();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Failed to update article read state: {ex}");
        }
    }
}
