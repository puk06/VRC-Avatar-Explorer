using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BulkImportPreset : UserControl
{
    public BulkImportPreset()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance.BulkImportPresetVM;
    }
}
