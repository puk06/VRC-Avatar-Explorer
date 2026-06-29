using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;
using Avalonia.Platform.Storage;
using AvatarExplorer.UI.ViewModels.Panels;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BulkImport : UserControl
{
    public BulkImport()
    {
        InitializeComponent();
    }

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
