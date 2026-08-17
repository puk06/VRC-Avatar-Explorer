using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class FetchAllVariationHashesOverlay : UserControl
{
    public FetchAllVariationHashesOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.FetchAllVariationHashesVM;
    }
}
