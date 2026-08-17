using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BulkImportPreset : UserControl
{
    public BulkImportPreset()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.BulkImportPresetVM;
    }
}
