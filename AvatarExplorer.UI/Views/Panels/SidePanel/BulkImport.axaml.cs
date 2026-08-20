using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;
using Avalonia.Platform.Storage;
using AvatarExplorer.UI.ViewModels.Panels;
using AvatarExplorer.UI.Services;
using System.Threading.Tasks;
using AvatarExplorer.UI.Interfaces;

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
    }

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
