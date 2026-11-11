using CommunityToolkit.Mvvm.ComponentModel;

namespace RssReader.App.ViewModels;

public partial class OpmlNotificationViewModel : ViewModelBase
{
	public string FileName { get; }

	[ObservableProperty]
	public partial string Message { get; set; }

	[ObservableProperty]
	public partial bool IsError { get; set; }

	public OpmlNotificationViewModel(string fileName, string message, bool isError)
	{
		FileName = fileName;
		Message = message;
		IsError = isError;
	}
}
