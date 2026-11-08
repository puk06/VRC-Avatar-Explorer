using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvatarExplorer.UI.ViewModels;
using AvatarExplorer.UI.ViewModels.Panels;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services.ContextMenu;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BulkImport : UserControl, IInitializable
{
    public BulkImport()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.BulkImportVM;

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        if (DataContext is BulkImportViewModel vm)
            vm.OnItemsAdded += ScrollToEnd;

        ContextMenuHandlerService.Register(ActionKey.RemovePreset, _ => PresetLoadButton.Flyout?.Hide());
    }

    private void OnPresetPickerOpening(object? sender, EventArgs e)
        => PresetPickerLayout.Width = Bounds.Width * MainWindowViewModel.Instance.UiScale;

    private void OnPresetSelected(object? sender, EventArgs e) => PresetLoadButton.Flyout?.Hide();

    private void ScrollToEnd() => ItemsScrollViewer.ScrollToEnd();

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!(e.DataTransfer.Contains(DataFormat.Text) || e.DataTransfer.Contains(DataFormat.File))) return;

        if (DataContext is BulkImportViewModel vm)
        {
            var text = e.DataTransfer.TryGetText();
            if (!string.IsNullOrEmpty(text)) vm.OnBulkImportDropped(text);

            var file = e.DataTransfer.TryGetFile()?.TryGetLocalPath();
            if (!string.IsNullOrEmpty(file)) vm.OnBulkImportDropped(file, isFile: true);
        }
    }
}
