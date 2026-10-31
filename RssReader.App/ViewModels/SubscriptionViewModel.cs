using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;

namespace RssReader.App.ViewModels;

public partial class SubscriptionViewModel : ViewModelBase
{
	public FeedSubscription Item { get; }

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