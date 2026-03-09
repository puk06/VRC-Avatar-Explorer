using Avalonia.Interactivity;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void Dialog_Show(string title, string content)
    {
        DialogTitle.Text = title;
        DialogContent.Text = content;

        DialogOverlay.IsVisible = true;
    }
    private void Dialog_Hide() => DialogOverlay.IsVisible = false;

    #region Event Handler
    private void Dialog_OK_Click(object? sender, RoutedEventArgs e) => Dialog_Hide();
    #endregion
}
