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