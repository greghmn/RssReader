// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using Avalonia.Controls;
using RssReader.App.Results;
using System;
using System.Threading.Tasks;

namespace RssReader.App.Views;

public partial class AddSubscriptionWindow : Window
{
    public string FeedUrl { get; private set; } = "";
    public string FeedTitle { get; private set; } = "";

    public Func<string, string, Task<AddSubscriptionResult>>? SubmitAsync { get; set; }
    

    public AddSubscriptionWindow()
    {
        InitializeComponent();
    }
    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private async void AddSubscriptionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FeedUrl = FeedUrlTextBox.Text?.Trim() ?? "";
        FeedTitle = FeedTitleTextBox.Text?.Trim() ?? "";
        
        ErrorTextBlock.IsVisible = false;

        if (string.IsNullOrWhiteSpace(FeedUrl))
        {
            ErrorTextBlock.Text = "Please enter a feed URL.";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        if (SubmitAsync is null)
        {
            ErrorTextBlock.Text = "Unable to submit the subscription.";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        AddSubscriptionButton.IsEnabled = false;

        try
        {
            AddSubscriptionResult result =
                await SubmitAsync(FeedUrl, FeedTitle);

            switch (result)
            {
                case AddSubscriptionResult.Success:
                    Close(true);
                    break;

                case AddSubscriptionResult.AlreadyExists:
                    ErrorTextBlock.Text =
                        "You're already subscribed to this feed.";
                    break;

                case AddSubscriptionResult.InvalidUrl:
                    ErrorTextBlock.Text =
                        "Please enter a valid HTTP or HTTPS feed URL.";
                    break;

                case AddSubscriptionResult.Failed:
                    ErrorTextBlock.Text =
                        "The subscription couldn't be saved. Please try again.";
                    break;
            }

            ErrorTextBlock.IsVisible =
                result != AddSubscriptionResult.Success;
        }
        finally
        {
            if (IsVisible)
            {
                AddSubscriptionButton.IsEnabled = true;
            }
        }
    }
}