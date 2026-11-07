using Avalonia;
using System;

namespace RssReader.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
//If the behavior is good, we'll next investigate how to make long titles, URLs, and status messages behave gracefully inside those constrained widths.
/*
 My proposed next phase is a focused GUI and persistence audit, followed by release packaging:
1. Verify that subscription and article state behaves correctly across application restarts.
2. Review remaining user-visible failures and misleading status messages.
3. Check minimum-window-size behavior and keyboard accessibility.
4. Prepare a Windows distributable.
5. Write the installation and usage sections of the README.
 */