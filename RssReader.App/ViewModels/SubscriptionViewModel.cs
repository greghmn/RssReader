using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;
using System;

namespace RssReader.App.ViewModels;

public partial class SubscriptionViewModel : ViewModelBase
{
	public FeedSubscription Item { get; }

	[ObservableProperty]
	public partial DateTimeOffset? LastRetrieved { get; set; }

	public string LastRetrievedText =>
		LastRetrieved.HasValue
				? $"Last retrieved: {LastRetrieved.Value.ToLocalTime():g}"
				: "Not yet retrieved";

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



	[ObservableProperty]
	public partial string Title { get; set; } = "";

	[ObservableProperty]
	public partial bool IsRefreshing { get; set; }

	[ObservableProperty]
	public partial string? RefreshError { get; set; }

	[ObservableProperty]
	public partial string? RefreshStatus { get; set; }

	public bool HasUnreadCount => UnreadCount.HasValue;

	partial void OnUnreadCountChanged(int? value)
	{
		OnPropertyChanged(nameof(HasUnreadCount));
	}

	public SubscriptionViewModel(FeedSubscription item)
	{
		Item = item;
		Title = item.Title;
		LastRetrieved = item.LastRetrieved;
	}

	partial void OnTitleChanged(string value)
	{
		Item.Title = value;
	}

	partial void OnLastRetrievedChanged(DateTimeOffset? value)
	{
		Item.LastRetrieved = value;
		OnPropertyChanged(nameof(LastRetrievedText));
	}
}