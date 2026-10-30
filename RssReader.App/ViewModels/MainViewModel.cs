using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using RssReader.App.Results;

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
    public partial FeedItem? SelectedArticle { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedArticle { get; set; }

    [ObservableProperty]
    public partial bool HasNoSelectedArticle { get; set; } = true;

    private readonly SubscriptionService _subscriptionService;

    private readonly FeedService _feedService;

    private int _articleLoadRequestId;

    public ObservableCollection<FeedSubscription> Subscriptions { get; } = new();

    public ObservableCollection<FeedItem> Articles { get; } = new();

    public MainViewModel(SubscriptionService subscriptionService, FeedService feedService)
    {
        _subscriptionService = subscriptionService;
        _feedService = feedService;
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

    public async Task LoadArticlesAsync(FeedSubscription subscription)
    {
        var articles = await _feedService.GetItemsAsync(subscription.Url);

        Articles.Clear();

        foreach (var article in articles)
        {
            Articles.Add(article);
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
                Articles.Add(article);
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
    partial void OnSelectedArticleChanged(FeedItem? value)
    {
        HasSelectedArticle = value is not null;
        HasNoSelectedArticle = value is null;
    }
}
