using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.Views;

public partial class MainView : UserControl
{
    private static readonly double[] GridItemImageSizes = [130, 170, 210];
    private static readonly double[] GridItemMinWidths = [150, 190, 230];
    private const int HoverOffset = 20;

    public static readonly StyledProperty<double> GridItemImageSizeProperty =
        AvaloniaProperty.Register<MainView, double>(nameof(GridItemImageSize), 170.0);

    public double GridItemImageSize
    {
        get => GetValue(GridItemImageSizeProperty);
        set => SetValue(GridItemImageSizeProperty, value);
    }

    private readonly HoverThumbnailWindow _hoverWindow = new();
    private readonly ObservableCollection<GridRow> _gridRows = [];
    private int _gridColumns = 1;
    private double _gridItemMinWidth = 190;

    public MainView()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView;

        RegisterSidePanelEvent();
        RegisterPathScrollEvent();
        RegisterScrollTracking();
        RegisterHoverThumbnailEvent();
        RegisterWindowClosingEvent();
        RegisterGridResizeEvent();
    }

    private void RegisterGridResizeEvent()
    {
        GridItemsControl.ItemsSource = _gridRows;
        GridItemsControl.SizeChanged += OnGridItemsControlSizeChanged;

        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnGridItemsPropertyChanged;
        }
    }

    private void OnGridItemsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.MainItems):
                RebuildGridRows();
                break;
            case nameof(MainViewModel.MainGridItemSize):
                UpdateGridItemSize();
                break;
        }
    }

    private void UpdateGridItemSize()
    {
        if (DataContext is not MainViewModel vm) return;

        var index = Math.Clamp(vm.MainGridItemSize, 0, GridItemImageSizes.Length - 1);
        GridItemImageSize = GridItemImageSizes[index];
        _gridItemMinWidth = GridItemMinWidths[index];
        UpdateGridColumns(GridItemsControl.Bounds.Width);
        RebuildGridRows();
    }

    private void OnGridItemsControlSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateGridColumns(e.NewSize.Width);
    }

    private void UpdateGridColumns(double width)
    {
        var columns = Math.Max(1, (int)(width / _gridItemMinWidth));
        if (columns == _gridColumns) return;
        _gridColumns = columns;
        RebuildGridRows();
    }

    private void RebuildGridRows()
    {
        if (DataContext is not MainViewModel vm) return;

        _gridRows.Clear();
        var items = vm.MainItems.ToList();
        for (var i = 0; i < items.Count; i += _gridColumns)
        {
            _gridRows.Add(new GridRow(items.Skip(i).Take(_gridColumns).ToList(), _gridColumns));
        }
    }

    private void RegisterWindowClosingEvent()
    {
        InstanceRepository.MainWindow.WindowClosing += CloseHoverWindow;
    }

    private void RegisterPathScrollEvent()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnPathSegmentsChanged;
        }

        PathScrollViewer.ScrollChanged += OnPathScrollViewerScrollChanged;
        PathScrollViewer.SizeChanged += OnPathScrollViewerSizeChanged;
    }

    private void OnPathSegmentsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PathSegments))
        {
            Dispatcher.UIThread.Post(UpdatePathOverflowAndScroll, DispatcherPriority.Render);
        }
    }

    private void OnPathScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentDelta != default || e.ViewportDelta != default)
        {
            UpdatePathOverflow();
        }
    }

    private void OnPathScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdatePathOverflowAndScroll();
    }

    private void UpdatePathOverflow()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.HasOverflow = PathScrollViewer.Extent.Width > PathScrollViewer.Viewport.Width;
        }
    }

    private void UpdatePathOverflowAndScroll()
    {
        UpdatePathOverflow();
        ScrollPathToEnd();
    }

    private void ScrollPathToEnd()
    {
        var maxOffset = Math.Max(0, PathScrollViewer.Extent.Width - PathScrollViewer.Viewport.Width);
        PathScrollViewer.Offset = new Vector(maxOffset, 0);
    }

    private void RegisterScrollTracking()
    {
        LeftPanelScrollViewer.ScrollChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.LeftPageInfo.ScrollOffset = LeftPanelScrollViewer.Offset;
        };

        RightPanelScrollViewer.ScrollChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RightPageInfo.ScrollOffset = RightPanelScrollViewer.Offset;
        };

        if (DataContext is MainViewModel mainVm)
        {
            mainVm.LeftPageInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.LeftPageInfo.RestoreScrollOffset))
                    LeftPanelScrollViewer.Offset = mainVm.LeftPageInfo.RestoreScrollOffset;
            };

            mainVm.RightPageInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RightPageInfo.RestoreScrollOffset))
                    RightPanelScrollViewer.Offset = mainVm.RightPageInfo.RestoreScrollOffset;
            };
        }
    }

    private void RegisterSidePanelEvent()
    {
        SidePanelTabControl.Items
            .OfType<TabItem>()
            .ForEach(i => i.AddHandler(
                PointerPressedEvent,
                SidePanelButton_OnPointerPressed,
                RoutingStrategies.Tunnel
            ));
    }

    private void SidePanelButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is TabItem tab)
        {
            int index = ValueParser.Int((string?)tab.Tag);
            vm.SidePanelButtonPressed(index);
        }
    }

    private void RegisterHoverThumbnailEvent()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnHoverThumbnailPropertyChanged;
        }
    }

    private void OnHoverThumbnailPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsHoverThumbnailVisible):
                OnHoverThumbnailVisibilityChanged(vm.IsHoverThumbnailVisible);
                break;
            case nameof(MainViewModel.HoverThumbnailImage):
                _hoverWindow.SetImage(vm.HoverThumbnailImage);
                break;
            case nameof(MainViewModel.HoverThumbnailPosition):
                _hoverWindow.Position = vm.HoverThumbnailPosition;
                break;
        }
    }

    private void OnHoverThumbnailVisibilityChanged(bool isVisible)
    {
        if (isVisible)
        {
            _hoverWindow.Show();
            _hoverWindow.SetSize(InstanceRepository.UserPreferences.HoverIconSize);
            _hoverWindow.Topmost = false;
            _hoverWindow.Topmost = true;
        }
        else
        {
            _hoverWindow.Hide();
        }
    }

    public void CloseHoverWindow() => _hoverWindow.Close();

    private void OnItemImageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image image) return;

        var bitmapInterpolationMode = InstanceRepository.UserPreferences.AntiAliasingMode.GetInterpolationMode();
        if (bitmapInterpolationMode != BitmapInterpolationMode.None && bitmapInterpolationMode != BitmapInterpolationMode.Unspecified)
            RenderOptions.SetBitmapInterpolationMode(image, bitmapInterpolationMode);

        image.PointerEntered += OnItemImagePointerEntered;
        image.PointerExited += OnItemImagePointerExited;
        image.PointerMoved += OnItemImagePointerMoved;
    }

    private void OnItemImageUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image image) return;
        image.PointerEntered -= OnItemImagePointerEntered;
        image.PointerExited -= OnItemImagePointerExited;
        image.PointerMoved -= OnItemImagePointerMoved;
    }

    private void OnItemButtonLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        button.AddHandler(PointerPressedEvent, OnItemButtonPointerPressed, RoutingStrategies.Tunnel);
    }
    private void OnItemButtonUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        button.RemoveHandler(PointerPressedEvent, OnItemButtonPointerPressed);
    }

    private void OnItemImagePointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Image image || DataContext is not MainViewModel vm) return;
        if (image.DataContext is not ItemViewModel item) return;

        vm.UpdateHoverThumbnailPosition(GetScreenPosition(e));
        vm.ShowHoverThumbnail(item);
    }

    private void OnToolTipOpening(object? sender, CancelRoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.IsHoverThumbnailVisible) e.Cancel = true;
    }

    private void OnItemImagePointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.HideHoverThumbnail();
    }

    private void OnItemImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.UpdateHoverThumbnailPosition(GetScreenPosition(e));
    }

    private PixelPoint GetScreenPosition(PointerEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not Window window) return default;
        var position = window.PointToScreen(e.GetPosition(window));
        return new PixelPoint(position.X + HoverOffset, position.Y + HoverOffset);
    }

    private async void OnItemButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ItemViewModel item) return;

        var transferItem = new DataTransferItem();
        string? droppedPath = null;

        switch (item.ViewModelType)
        {
            case ViewModelType.File:
                if (string.IsNullOrEmpty(item.ActualValue)) return;
                var storageFile = await StorageService.GetStorageFileFromPath(item.ActualValue);
                if (storageFile != null)
                {
                    transferItem.Set(DataFormat.File, storageFile);
                    droppedPath = item.ActualValue;
                }
                else
                {
                    transferItem.Set(DataFormat.Text, item.ActualValue);
                }
                break;
            case ViewModelType.Item:
                if (string.IsNullOrEmpty(item.Identifier)) return;
                transferItem.Set(DataFormat.Text, item.Identifier);
                break;
            case ViewModelType.Avatar:
            case ViewModelType.Folder:
                if (string.IsNullOrEmpty(item.ActualValue)) return;
                transferItem.Set(DataFormat.Text, item.ActualValue);
                break;
            default:
                return;
        }

        var dragData = new DataTransfer();
        dragData.Add(transferItem);

        await Task.Delay(300);

        if (!button.IsPressed) return;

        InstanceRepository.MainWindow.LastDragDropPath = droppedPath;
        await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Copy);
    }
    private async void OnMainPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerProperties = e.GetCurrentPoint(this).Properties;
        var sideButtonPressed = pointerProperties.IsXButton1Pressed;

        if (sideButtonPressed && DataContext is MainViewModel vm)
        {
            vm.Undo();
        }
    }
}
