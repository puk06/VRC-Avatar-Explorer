using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using ReactiveUI.Builder;

namespace AvatarExplorer.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async override void OnFrameworkInitializationCompleted()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();

        AppInitializer.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            TopLevelProvider.Current = TopLevel.GetTopLevel(desktop.MainWindow);
            if (desktop.MainWindow is MainWindow mw) mw.SendApplicationArgs(desktop.Args);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
