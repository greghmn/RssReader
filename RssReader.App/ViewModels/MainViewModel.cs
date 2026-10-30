using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace RssReader.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial FeedSubscription? SelectedSubscription { get; set; }
    
    [ObservableProperty]
    public partial string? ArticleLoadError { get; set; }

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
    public async Task LoadArticlesAsync(FeedSubscription subscription)
    {
        var articles = await _feedService.GetItemsAsync(subscription.Url);

        Articles.Clear();

        foreach (var article in articles)
        {
            Articles.Add(article);
        }
    }
    partial void OnSelectedSubscriptionChanged(
        FeedSubscription? value)
    {
        if (value is not null)
        {
            _ = LoadArticlesSafelyAsync(value);
        }
        else
        {
            ++_articleLoadRequestId;
            Articles.Clear();
            ArticleLoadError = null;
        }
    }
    private async Task LoadArticlesSafelyAsync(
        FeedSubscription subscription)
    {
        int requestId = ++_articleLoadRequestId;

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
    }
}
