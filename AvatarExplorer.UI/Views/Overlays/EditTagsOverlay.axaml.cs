using Avalonia.Controls;
using Avalonia.Input;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.ViewModels.Overlays;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditTagsOverlay : UserControl
{
    public EditTagsOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.EditTagsVM;
    }

    private void OnTagBorderClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is string tag)
        {
            if (DataContext is EditTagsViewModel vm)
                vm.OnTagClick(tag);
        }
    }

    private void OnExistTagSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is EditTagsViewModel vm)
            vm.OnExistTagSelectionChanged();
    }
}
