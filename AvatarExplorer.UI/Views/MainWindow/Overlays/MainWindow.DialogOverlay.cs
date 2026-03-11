using Avalonia.Interactivity;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void DialogOverlay_Show(string title, string content)
    {
        DialogTitle.Text = title;
        DialogContent.Text = content;

        DialogOverlay.IsVisible = true;
    }
    private void DialogOverlay_Hide() => DialogOverlay.IsVisible = false;

    #region Event Handler
    private void DialogOverlay_OK_Click(object? sender, RoutedEventArgs e) => DialogOverlay_Hide();
    #endregion
}
