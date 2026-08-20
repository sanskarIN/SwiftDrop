using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace SwiftDrop.Desktop;

public sealed partial class App : Application
{
    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            window.ApplyLaunchArguments(Environment.GetCommandLineArgs().Skip(1));
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
