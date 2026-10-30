using Avalonia.Interactivity;
using RssReader.App.ViewModels;
using Avalonia.Controls;

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

        var dialog = new AddSubscriptionWindow();

        bool result = await dialog.ShowDialog<bool>(this);

        if (!result)
        {
            return;
        }

        await viewModel.AddSubscriptionAsync(
            dialog.FeedUrl,
            dialog.FeedTitle);
    }
}