// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using RssReader.App.ViewModels;
using RssReader.App.Views;
using RssReader.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
namespace RssReader.App.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();

		ArticleList.AddHandler(InputElement.PointerPressedEvent, ArticleList_PointerPressed, RoutingStrategies.Tunnel);
		SubscriptionList.AddHandler(InputElement.PointerPressedEvent, SubscriptionList_PointerPressed, RoutingStrategies.Tunnel);
		AddHandler(InputElement.KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);
	}
	private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.W
				&& e.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			Close();
			e.Handled = true;
		}
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
		var properties =
						e.GetCurrentPoint(SubscriptionList).Properties;

		if (properties.IsRightButtonPressed)
		{
			e.Handled = true;
			return;
		}

		if (!properties.IsLeftButtonPressed)
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
	private async void RenameSubscriptionMenuItem_Click(object? sender, RoutedEventArgs e)
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
	private async void RemoveSubscriptionMenuItem_Click(object? sender, RoutedEventArgs e)
	{
		if (sender is not MenuItem menuItem)
		{
			return;
		}

		if (menuItem.DataContext is not SubscriptionViewModel subscription)
		{
			return;
		}

		if (DataContext is not MainViewModel viewModel)
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
	private async void ImportOpmlButton_Click(object? sender, RoutedEventArgs e)
	{
		IReadOnlyList<IStorageFile> files;

		try
		{
			files = await StorageProvider.OpenFilePickerAsync(
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
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine(
							$"Unable to open OPML file picker: {ex}");

			if (DataContext is MainViewModel vm)
			{
				vm.ShowOpmlNotification(
								"File selection",
								"Unable to open the file picker.",
								true);
			}

			return;
		}

		if (files.Count == 0)
		{
			return;
		}

		if (DataContext is not MainViewModel viewModel)
		{
			return;
		}

		string fileName = files[0].Name;

		viewModel.OpmlImportStatus = null;
		viewModel.OpmlImportError = null;

		try
		{
			await using Stream stream = await files[0].OpenReadAsync();

			using var reader = new StreamReader(
					stream,
					Encoding.UTF8,
					detectEncodingFromByteOrderMarks: true);

			string xml = await reader.ReadToEndAsync();

			List<OpmlImportEntry> entries =
							await viewModel.PrepareOpmlImportAsync(xml);

			var dialog = new OpmlImportPreviewWindow(entries);

			bool confirmed = await dialog.ShowDialog<bool>(this);

			if (confirmed)
			{
				List<OpmlImportExecutionResult> results =
								await viewModel.ExecuteOpmlImportAsync(entries);

				string message = viewModel.OpmlImportStatus
								?? "No new subscriptions imported.";

				bool hasFailures = results.Any(result =>
								result.Status == OpmlImportExecutionStatus.Failed);

				viewModel.ShowOpmlNotification(fileName, message, hasFailures);
			}

		}
		catch (IOException)
		{
			viewModel.ShowOpmlNotification(fileName, "Unable to read the file.", true);
		}
		catch (UnauthorizedAccessException)
		{
			viewModel.ShowOpmlNotification(fileName, "Permission denied while reading the file.", true);
		}
		catch (XmlException)
		{
			viewModel.ShowOpmlNotification(fileName, "The file contains malformed XML.", true);
		}
		catch (FormatException)
		{
			viewModel.ShowOpmlNotification(fileName, "Not a valid OPML document.", true);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine(
							$"Unexpected OPML import error ({fileName}): {ex}");

			viewModel.ShowOpmlNotification(
							fileName,
							"An unexpected error occurred during import. See diagnostic output for details.",
							true);
		}
	}
	private async void ExportOpmlButton_Click(object? sender, RoutedEventArgs e)
	{
		if (DataContext is not MainViewModel viewModel)
		{
			return;
		}

		if (viewModel.IsExportingOpml)
		{
			return;
		}

		viewModel.IsExportingOpml = true;
		viewModel.OpmlExportStatus = null;
		viewModel.OpmlExportError = null;

		try
		{
			var file = await StorageProvider.SaveFilePickerAsync(
					new FilePickerSaveOptions
					{
						Title = "Export OPML subscriptions",
						SuggestedFileName = "subscriptions.opml",
						DefaultExtension = "opml",
						ShowOverwritePrompt = true,
						FileTypeChoices =
							[
									new FilePickerFileType("OPML files")
										{
												Patterns = ["*.opml"]
										}
							]
					});

			if (file is null)
			{
				return;
			}

			string xml = await viewModel.ExportOpmlAsync();

			await using Stream stream = await file.OpenWriteAsync();

			await using var writer = new StreamWriter(
					stream,
					new UTF8Encoding(
							encoderShouldEmitUTF8Identifier: false));


			await writer.WriteAsync(xml);

			viewModel.OpmlExportStatus =
					"Subscriptions exported successfully.";
		}
		catch (UnauthorizedAccessException)
		{
			viewModel.OpmlExportError =
					"Permission denied while exporting subscriptions.";
		}
		catch (IOException)
		{
			viewModel.OpmlExportError =
					"Unable to write the OPML file.";
		}
		catch (Exception ex)
		{
			viewModel.OpmlExportError =
					$"Unable to export subscriptions: {ex.Message}";
		}
		finally
		{
			viewModel.IsExportingOpml = false;
		}
	}
	private async void OpenFeedUrlMenuItem_Click(object? sender, RoutedEventArgs e)
	{
		if (sender is not MenuItem menuItem)
		{
			return;
		}

		if (menuItem.DataContext is not SubscriptionViewModel subscription)
		{
			return;
		}

		Uri? uri = subscription.FeedUri;

		if (uri is null)
		{
			return;
		}

		await TopLevel.GetTopLevel(this)!
						.Launcher.LaunchUriAsync(uri);
	}
	private async void RefreshSubscriptionMenuItem_Click(object? sender, RoutedEventArgs e)
	{
		if (sender is not MenuItem menuItem)
		{
			return;
		}

		if (menuItem.DataContext is not SubscriptionViewModel subscription)
		{
			return;
		}

		if (DataContext is not MainViewModel viewModel)
		{
			return;
		}

		if (ReferenceEquals(
						viewModel.SelectedSubscription,
						subscription))
		{
			await viewModel.RefreshSelectedFeedCommand.ExecuteAsync(null);
		}
		else
		{
			await viewModel.RefreshSubscriptionInBackgroundAsync(
							subscription);
		}
	}
}
