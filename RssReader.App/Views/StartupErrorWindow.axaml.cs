// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RssReader.App.Views;

public partial class StartupErrorWindow : Window
{
	public StartupErrorWindow()
	{
		InitializeComponent();
	}

	private void CloseButton_Click(
			object? sender,
			RoutedEventArgs e)
	{
		Close();
	}
}
