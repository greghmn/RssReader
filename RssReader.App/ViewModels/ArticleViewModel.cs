using CommunityToolkit.Mvvm.ComponentModel;
using RssReader.Core;

namespace RssReader.App.ViewModels;

public partial class ArticleViewModel : ViewModelBase
{
    public FeedItem Item { get; }

    [ObservableProperty]
    public partial bool IsRead { get; set; }

    public ArticleViewModel(FeedItem item, bool isRead)
    {
        Item = item;
        IsRead = isRead;
    }
}