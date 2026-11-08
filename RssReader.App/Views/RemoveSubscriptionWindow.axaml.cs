// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
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