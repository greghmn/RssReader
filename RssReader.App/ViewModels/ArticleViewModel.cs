// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;
using System;

namespace RssReader.App.ViewModels;

public partial class ArticleViewModel : ViewModelBase
{
	public FeedItem Item { get; }

	[ObservableProperty]
	public partial bool IsRead { get; set; }

	public bool CanOpenArticleLink
	{
		get
		{
			return Uri.TryCreate(
								 Item.Link,
								 UriKind.Absolute,
								 out Uri? uri)
						 && (uri.Scheme == Uri.UriSchemeHttp
								 || uri.Scheme == Uri.UriSchemeHttps);
		}
	}

	public string DisplayContent =>
		!string.IsNullOrWhiteSpace(Item.Content)
				? Item.Content
				: Item.Summary;

	public bool IsUnread => !IsRead;

	public string PublishedDateText =>
		Item.PublishedAt?.ToLocalTime().ToString("g")
		?? "Publication date unknown";

	public ArticleViewModel(FeedItem item, bool isRead)
	{
		Item = item;
		IsRead = isRead;
	}

	partial void OnIsReadChanged(bool value)
	{
		OnPropertyChanged(nameof(IsUnread));
	}
}