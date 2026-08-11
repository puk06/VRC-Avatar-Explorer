using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class FetchAllVariationHashesOverlay : UserControl
{
    public FetchAllVariationHashesOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.FetchAllVariationHashesVM;
    }
}
