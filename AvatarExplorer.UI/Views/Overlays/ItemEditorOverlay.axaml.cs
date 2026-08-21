using Avalonia.Controls;
using Avalonia.Input;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.ViewModels.Overlays;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ItemEditorOverlay : UserControl
{
    public ItemEditorOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ItemEditorVM;
    }

    private async void OnBoothUrlKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ItemEditorViewModel vm) return;

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Enter)
        {
            await vm.FetchBoothData();
        }
    }

    private async void OnItemEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ItemEditorViewModel vm) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Enter)
        {
            await vm.Confirm();
        }
    }
}
