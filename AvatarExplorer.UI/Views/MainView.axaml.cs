using System;
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
    private readonly HoverThumbnailWindow _hoverWindow = new();

    public MainView()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView;

        RegisterSidePanelEvent();
        RegisterPathScrollEvent();
        RegisterScrollTracking();
        RegisterHoverThumbnailEvent();
        RegisterWindowClosingEvent();
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
        else _hoverWindow.Hide();
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

    private const int HoverOffset = 20;

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

        var viewModelType = item.ViewModelType;
        
        if (viewModelType != ViewModelType.Item &&
            viewModelType != ViewModelType.Avatar &&
            viewModelType != ViewModelType.File &&
            viewModelType != ViewModelType.Folder) return;

        var transferItem = new DataTransferItem();
        string? droppedPath = null;

        if (viewModelType == ViewModelType.File && !string.IsNullOrEmpty(item.ActualValue))
        {
            var storageFile = await StorageService.GetStorageFileFromPath(item.ActualValue);
            if (storageFile != null)
            {
                transferItem.Set(DataFormat.File, storageFile);
                droppedPath = item.ActualValue;
            }
            else transferItem.Set(DataFormat.Text, item.ActualValue);
        }
        else if (viewModelType == ViewModelType.Item && !string.IsNullOrEmpty(item.Identifier))
        {
            transferItem.Set(DataFormat.Text, item.Identifier);
        }
        else if (viewModelType == ViewModelType.Avatar && !string.IsNullOrEmpty(item.ActualValue))
        {
            transferItem.Set(DataFormat.Text, item.ActualValue);
        }
        else if (viewModelType == ViewModelType.Folder && !string.IsNullOrEmpty(item.ActualValue))
        {
            transferItem.Set(DataFormat.Text, item.ActualValue);
        }
        else
        {
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
