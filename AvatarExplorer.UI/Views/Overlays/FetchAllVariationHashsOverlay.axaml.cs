using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class FetchAllVariationHashsOverlay : UserControl
{
    public FetchAllVariationHashsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.FetchAllVariationHashsVM;
    }
}
