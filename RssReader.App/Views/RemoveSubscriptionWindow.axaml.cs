using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RssReader.App.Views;

public partial class RemoveSubscriptionWindow : Window
{
	public RemoveSubscriptionWindow(string subscriptionTitle)
	{
		InitializeComponent();

		SubscriptionTitleTextBlock.Text =
				$"You are about to remove \"{subscriptionTitle}\".";
	}

	private void CancelButton_Click(
			object? sender,
			RoutedEventArgs e)
	{
		Close(false);
	}

	private void ConfirmButton_Click(
			object? sender,
			RoutedEventArgs e)
	{
		Close(true);
	}
}