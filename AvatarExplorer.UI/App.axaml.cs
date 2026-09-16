using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Services.System;

namespace AvatarExplorer.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RegisterInputHandlers();
    }

    private static void RegisterInputHandlers()
    {
        // Prevent ComboBox from scrolling when mouse wheel is used over it
        InputElement.PointerWheelChangedEvent.AddClassHandler<ComboBox>(
            (_, e) => e.Handled = true,
            RoutingStrategies.Tunnel
        );
    }

    public async override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            TopLevelProvider.Current = TopLevel.GetTopLevel(desktop.MainWindow);

            await IInitializableRegistry.Complete();

            if (desktop.MainWindow is MainWindow mw)
                mw.SetApplicationArgs(desktop.Args);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
