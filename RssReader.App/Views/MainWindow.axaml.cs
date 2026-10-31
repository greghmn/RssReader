using Avalonia.Controls;
using Avalonia.Interactivity;
using RssReader.App.ViewModels;
using RssReader.Core;
using System;

namespace RssReader.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void AddSubscriptionButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new AddSubscriptionWindow
        {
            SubmitAsync = viewModel.AddSubscriptionAsync
        };

        bool result = await dialog.ShowDialog<bool>(this);

        if (!result)
        {
            return;
        }

        await viewModel.AddSubscriptionAsync(
            dialog.FeedUrl,
            dialog.FeedTitle);
    }

    private async void RemoveSubscriptionButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        SubscriptionViewModel? subscription =
            viewModel.SelectedSubscription;

        if (subscription is null)
        {
            return;
        }

        var dialog = new RemoveSubscriptionWindow(
            subscription.Item.Title);

        bool confirmed = await dialog.ShowDialog<bool>(this);

        if (!confirmed)
        {
            return;
        }

        try
        {
            bool removed =
                await viewModel.RemoveSubscriptionAsync(subscription);

            if (!removed)
            {
                Console.Error.WriteLine(
                    $"Subscription not found during removal: {subscription.Item.Url}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Failed to remove subscription {subscription.Item.Url}: {ex}");
        }
    }
}