using Avalonia.Interactivity;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void ErrorLogOverlay_Open()
    {
        ErrorLogOverlay_RefleshLogs();
        ErrorLogOverlay.IsVisible = true;
    }
    private void ErrorLogOverlay_Close()
    {
        ErrorLogOverlay.IsVisible = false;
        ErrorLogOverlay_ErrorLogGrid.ItemsSource = null;
    }

    private void ErrorLogOverlay_RefleshLogs()
    {
        if (ErrorLogOverlay_ErrorLogGrid == null) return;

        ErrorLogOverlay_ErrorLogGrid.ItemsSource = null;
        ErrorLogOverlay_ErrorLogGrid.ItemsSource = ErrorManager.Instance.ErrorContexts;
    }

    #region Event Handler
    private void ErrorLogOverlay_Reflesh_Click(object? sender, RoutedEventArgs e) => ErrorLogOverlay_RefleshLogs();
    private void ErrorLogOverlay_Close_Click(object? sender, RoutedEventArgs e) => ErrorLogOverlay_Close();
    #endregion
}
