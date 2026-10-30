using Avalonia.Controls;

namespace RssReader.App.Views;

public partial class AddSubscriptionWindow : Window
{
    public string FeedUrl { get; private set; } = "";
    public string FeedTitle { get; private set; } = "";

    public AddSubscriptionWindow()
    {
        InitializeComponent();
    }
    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void AddSubscriptionButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        FeedUrl = FeedUrlTextBox.Text?.Trim() ?? "";
        FeedTitle = FeedTitleTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(FeedUrl))
        {
            return;
        }

        Close(true);
    }
}