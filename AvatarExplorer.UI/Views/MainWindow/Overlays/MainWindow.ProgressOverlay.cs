using System;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void ProgressOverlay_Show(string title, int value = -1)
    {
        if (ProgressOverlay.IsVisible)
        {
            ProgressOverlay_Title.Text = title;
            return;
        }

        ProgressOverlay_Title.Text = title;
        ProgressOverlay.IsVisible = true;

        if (value != -1) ProgressOverlay_Update(value);
    }
    private void ProgressOverlay_Hide() => ProgressOverlay.IsVisible = false;
    
    private void ProgressOverlay_Update(int value)
    {
        if (!ProgressOverlay.IsVisible) return;
        
        ProgressOverlay_Bar.IsIndeterminate = value == 0;
        ProgressOverlay_Bar.Value = Math.Clamp(value, 0, 100);
    }
}
