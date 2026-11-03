using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;
using System;

namespace RssReader.App.ViewModels;

public partial class SubscriptionViewModel : ViewModelBase
{
	public FeedSubscription Item { get; }

	public Uri? FeedUri
	{
		get
		{
			if (Uri.TryCreate(Item.Url, UriKind.Absolute, out var uri)
					&& (uri.Scheme == Uri.UriSchemeHttp
							|| uri.Scheme == Uri.UriSchemeHttps))
			{
				return uri;
			}

			return null;
		}
	}

	[ObservableProperty]
	public partial int? UnreadCount { get; set; }

	public bool HasUnreadCount => UnreadCount.HasValue;

	partial void OnUnreadCountChanged(int? value)
	{
		OnPropertyChanged(nameof(HasUnreadCount));
	}

	public SubscriptionViewModel(FeedSubscription item)
	{
		Item = item;
	}
}