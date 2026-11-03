using Avalonia.Controls;
using Avalonia.Interactivity;
using RssReader.App.ViewModels;
using RssReader.Core;
using System;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace RssReader.App.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();

		ArticleList.AddHandler(InputElement.PointerPressedEvent, ArticleList_PointerPressed, RoutingStrategies.Tunnel);
		SubscriptionList.AddHandler(InputElement.PointerPressedEvent, SubscriptionList_PointerPressed, RoutingStrategies.Tunnel);
	}

	private async void AddSubscriptionButton_Click(object? sender, RoutedEventArgs e)
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
	}

	private async void RemoveSubscriptionButton_Click(object? sender, RoutedEventArgs e)
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

		await viewModel.RemoveSubscriptionAsync(subscription);
	}
	private async void OpenArticleLinkButton_Click(object? sender, RoutedEventArgs e)
	{
		if (DataContext is not MainViewModel viewModel)
			return;

		ArticleViewModel? article = viewModel.SelectedArticle;

		if (article is null)
			return;

		if (!Uri.TryCreate(
						article.Item.Link,
						UriKind.Absolute,
						out Uri? uri))
		{
			return;
		}

		if (uri.Scheme != Uri.UriSchemeHttp &&
				uri.Scheme != Uri.UriSchemeHttps)
		{
			return;
		}

		try
		{
			var launcher = TopLevel.GetTopLevel(this)?.Launcher;

			if (launcher is null)
				return;

			bool launched = await launcher.LaunchUriAsync(uri);

			if (!launched)
			{
				Console.Error.WriteLine(
						$"Unable to open article URL: {uri}");
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
					$"Failed to open article URL: {ex}");
		}
	}
	private void ArticleList_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is not ListBox listBox)
			return;

		Control? source = e.Source as Control;

		while (source is not null && source is not ListBoxItem)
		{
			source = source.GetVisualParent() as Control;
		}

		if (source is ListBoxItem item)
		{
			if (item.DataContext is ArticleViewModel article
					&& ReferenceEquals(listBox.SelectedItem, article))
			{
				if (DataContext is MainViewModel viewModel)
				{
					viewModel.ClearArticleSelectionCommand.Execute(null);
				}

				// Prevent the ListBox from immediately selecting it again.
				e.Handled = true;
			}

			return;
		}

		if (DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.ClearArticleSelectionCommand.Execute(null);
		}
	}

	private void SubscriptionList_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is not ListBox listBox)
			return;

		Control? source = e.Source as Control;

		while (source is not null && source is not ListBoxItem)
		{
			source = source.GetVisualParent() as Control;
		}

		if (source is ListBoxItem item)
		{
			if (item.DataContext is SubscriptionViewModel subscription
					&& ReferenceEquals(listBox.SelectedItem, subscription))
			{
				if (DataContext is MainViewModel viewModel)
				{
					viewModel.ClearSubscriptionSelectionCommand.Execute(null);
				}

				e.Handled = true;
			}

			return;
		}

		if (DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.ClearSubscriptionSelectionCommand.Execute(null);
		}
	}
}