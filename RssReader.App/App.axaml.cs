using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RssReader.App.ViewModels;
using RssReader.App.Views;
using System;
using System.IO;
using RssReader.Core;
using System.Threading.Tasks;

namespace RssReader.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        string appDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RssReader");

        Directory.CreateDirectory(appDataDirectory);

        string databasePath = Path.Combine(
            appDataDirectory,
            "subscriptions.db");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = StartApplicationAsync(desktop, databasePath);
        }

        base.OnFrameworkInitializationCompleted();
    }
    private async Task StartApplicationAsync(
      IClassicDesktopStyleApplicationLifetime desktop,
      string databasePath)
    {
        try
        {
            var subscriptionRepository = new SqliteSubscriptionRepository(databasePath);

            await subscriptionRepository.InitializeAsync();

            var subscriptionService =
                new SubscriptionService(subscriptionRepository);
            
            var articleStateRepository = new SqliteArticleStateRepository(databasePath);

            await articleStateRepository.InitializeAsync();

            var articleStateService = new ArticleStateService(articleStateRepository);
            
            IFeedReader feedReader = new FeedReader();

            var feedService = new FeedService(subscriptionService, feedReader);

            var mainViewModel = new MainViewModel(subscriptionService, feedService, articleStateService);

            await mainViewModel.LoadSubscriptionsAsync();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application startup failed: {ex}");

            desktop.Shutdown(-1);
        }
    }
}