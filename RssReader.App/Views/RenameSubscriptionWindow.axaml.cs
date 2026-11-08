// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;

namespace RssReader.App.Views;

public partial class RenameSubscriptionWindow : Window
{
	public string NewTitle { get; private set; } = "";

	public Func<string, Task<bool>>? SubmitAsync { get; set; }

	public RenameSubscriptionWindow(string currentTitle)
	{
		InitializeComponent();

		TitleTextBox.Text = currentTitle;
		TitleTextBox.SelectAll();
		TitleTextBox.Focus();
	}

	private async void SaveButton_Click(object? sender, RoutedEventArgs e)
	{
		NewTitle = TitleTextBox.Text?.Trim() ?? "";

		if (string.IsNullOrWhiteSpace(NewTitle))
			return;

		if (SubmitAsync is null)
			return;


		SaveButton.IsEnabled = false;

		try
		{
			bool success = await SubmitAsync(NewTitle);

			if (success)
			{
				Close(true);
			}
			else
			{
				ErrorTextBlock.Text =
						"Unable to rename the subscription. Please try again.";

				ErrorTextBlock.IsVisible = true;
			}
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}
	private void CancelButton_Click(
			object? sender,
			RoutedEventArgs e)
	{
		Close(false);
	}

}