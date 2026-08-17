using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Panels;

public partial class AdvancedSearch : UserControl
{
    public AdvancedSearch()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.AdvancedSearchVM;
    }
}
