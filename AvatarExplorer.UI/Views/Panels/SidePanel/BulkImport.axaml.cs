using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.ViewModels;
using AvatarExplorer.UI.ViewModels.Panels;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BulkImport : UserControl, IInitializable
{
    public BulkImport()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.BulkImportVM;

        InitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        if (DataContext is BulkImportViewModel vm)
        {
            vm.OnItemsAdded += ScrollToEnd;
            vm.OnPresetSaveStarted += OnPresetSaveStarted;
        }

        ContextMenuHandlerService.Register(ActionKey.RemovePreset, _ => PresetButton.Flyout?.Hide());
    }

    private void OnPresetPickerOpening(object? sender, EventArgs e)
        => PresetPickerLayout.Width = PresetButton.Bounds.Width * (MainWindowViewModel.Instance?.UiScale ?? 1.0);

    #pragma warning disable IDE0051 // Remove unused private members (XAML側で使用されてる)
    private void OnPresetSelected() => PresetButton.Flyout?.Hide();
    #pragma warning restore IDE0051

    private void OnPresetSaveStarted() => PresetButton.Flyout?.Hide();

    private void ScrollToEnd() => ItemsScrollViewer.ScrollToEnd();

    private void OnItemImageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image image) return;

        var bitmapInterpolationMode = InstanceRepository.UserPreferences.AntiAliasingMode.GetInterpolationMode();
        if (bitmapInterpolationMode != BitmapInterpolationMode.None && bitmapInterpolationMode != BitmapInterpolationMode.Unspecified)
            RenderOptions.SetBitmapInterpolationMode(image, bitmapInterpolationMode);
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
