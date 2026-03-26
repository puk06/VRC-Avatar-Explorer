using Avalonia.Interactivity;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void DialogOverlay_Show(string title, string content)
    {
        DialogOverlay_Title.Text = title;
        DialogOverlay_Content.Text = content;

        DialogOverlay.IsVisible = true;
    }
    private void DialogOverlay_Hide()
    {
        DialogOverlay.IsVisible = false;
        DialogOverlay_Title.Text = string.Empty;
        DialogOverlay_Content.Text = string.Empty;
    }

    #region Event Handler
    private void DialogOverlay_OK_Click(object? sender, RoutedEventArgs e) => DialogOverlay_Hide();
    #endregion
}
