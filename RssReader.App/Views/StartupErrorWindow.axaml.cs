
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
