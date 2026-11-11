using Avalonia.Interactivity;
using Avalonia.Controls;
using RssReader.Core;
using System.Collections.Generic;

namespace RssReader.App.Views;

public partial class OpmlImportPreviewWindow : Window
{
	public OpmlImportPreviewWindow(
					IReadOnlyList<OpmlImportEntry> entries)
	{
		InitializeComponent();

		SubscriptionList.ItemsSource = entries;
	}
	private void CancelButton_Click(
				object? sender,
				RoutedEventArgs e)
	{
		Close(false);
	}
	private void ImportButton_Click(
				object? sender,
				RoutedEventArgs e)
	{
		Close(true);
	}
}
