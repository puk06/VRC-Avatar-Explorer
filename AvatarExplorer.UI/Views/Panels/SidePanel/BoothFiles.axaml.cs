using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BoothFiles : UserControl
{
    public BoothFiles()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.BoothFilesVM;
    }
}
