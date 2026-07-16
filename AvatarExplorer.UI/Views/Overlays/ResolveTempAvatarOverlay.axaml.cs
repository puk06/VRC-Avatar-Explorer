using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ResolveTempAvatarOverlay : UserControl
{
    public ResolveTempAvatarOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ResolveTempAvatarVM;
    }
}
