using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
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
        InputElement.PointerWheelChangedEvent.AddClassHandler<ComboBox>(
            OnComboBoxPointerWheelChanged,
            RoutingStrategies.Tunnel
        );
    }
    private static void OnComboBoxPointerWheelChanged(ComboBox comboBox, PointerWheelEventArgs e)
    {
        // ComboBox自体のホイールでの選択切り替えだけを止める
        e.Handled = true;

        // 代わりに、祖先にScrollViewerがあれば代理でスクロールする
        var scrollViewer = comboBox.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer is null) return;

        if (e.Delta.Y < 0)
            scrollViewer.LineDown();
        else if (e.Delta.Y > 0)
            scrollViewer.LineUp();
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
