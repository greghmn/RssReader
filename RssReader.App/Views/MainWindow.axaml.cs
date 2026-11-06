using Avalonia.Controls;
using Avalonia.Interactivity;
using RssReader.App.ViewModels;
using RssReader.App.Views;
using RssReader.Core;
using System;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Platform.Storage;
using System.IO;
using System.Text;

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
		if (!e.GetCurrentPoint(SubscriptionList)
				.Properties.IsLeftButtonPressed)
		{
			return;
		}
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

	private async void RenameSubscriptionMenuItem_Click(
			object? sender,
			RoutedEventArgs e)
	{
		if (sender is not MenuItem menuItem)
			return;

		if (menuItem.DataContext is not SubscriptionViewModel subscription)
			return;

		if (DataContext is not MainViewModel viewModel)
			return;

		var dialog = new RenameSubscriptionWindow(subscription.Title)
		{
			SubmitAsync = newTitle =>
					viewModel.RenameSubscriptionAsync(subscription, newTitle)
		};

		await dialog.ShowDialog<bool>(this);
	}

	private async void ImportOpmlButton_Click(
			object? sender,
			RoutedEventArgs e)
	{
		var files = await StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					Title = "Import OPML subscriptions",
					AllowMultiple = false,
					FileTypeFilter =
						[
								new FilePickerFileType("OPML files")
								{
										Patterns = ["*.opml", "*.xml"]
								}
						]
				});

		if (files.Count == 0)
		{
			return;
		}

		if (DataContext is not MainViewModel viewModel)
		{
			return;
		}

		try
		{
			await using Stream stream = await files[0].OpenReadAsync();

			using var reader = new StreamReader(
					stream,
					Encoding.UTF8,
					detectEncodingFromByteOrderMarks: true);

			string xml = await reader.ReadToEndAsync();

			await viewModel.ImportOpmlCommand.ExecuteAsync(xml);
		}
		catch (IOException)
		{
			viewModel.OpmlImportStatus = null;
			viewModel.OpmlImportError =
					"Unable to read the selected OPML file.";
		}
		catch (UnauthorizedAccessException)
		{
			viewModel.OpmlImportStatus = null;
			viewModel.OpmlImportError =
					"Permission denied while reading the selected OPML file.";
		}
	}
}