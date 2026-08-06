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
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.Views;

public partial class MainView : UserControl
{
    private HoverThumbnailWindow? _hoverWindow;

    public MainView()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.MainVM;

        RegisterSidePanelEvent();
        RegisterCategoryTabEvent();
        RegisterPathScrollEvent();
        RegisterScrollTracking();
        RegisterHoverThumbnailEvent();
        RegisterWindowClosingEvent();
    }

    private void RegisterWindowClosingEvent()
    {
        MainWindowViewModel.Instance.WindowClosing += CloseHoverWindow;
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
        Main_LeftPanelScrollViewer.ScrollChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.LeftPageInfo.ScrollOffset = Main_LeftPanelScrollViewer.Offset;
        };

        Main_RightPanelScrollViewer.ScrollChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RightPageInfo.ScrollOffset = Main_RightPanelScrollViewer.Offset;
        };

        if (DataContext is MainViewModel mainVm)
        {
            mainVm.LeftPageInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.LeftPageInfo.RestoreScrollOffset))
                    Main_LeftPanelScrollViewer.Offset = mainVm.LeftPageInfo.RestoreScrollOffset;
            };

            mainVm.RightPageInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RightPageInfo.RestoreScrollOffset))
                    Main_RightPanelScrollViewer.Offset = mainVm.RightPageInfo.RestoreScrollOffset;
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

    private void RegisterCategoryTabEvent()
    {
        CategoryTabControl.SelectionChanged += OnCategorySelectionChanged;
    }

    private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is TabControl tab)
        {
            vm.OnCategoryChanged(tab.SelectedIndex);
        }
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
                _hoverWindow?.SetImage(vm.HoverThumbnailImage);
                break;
            case nameof(MainViewModel.HoverThumbnailSize):
                _hoverWindow?.SetSize(vm.HoverThumbnailSize);
                break;
            case nameof(MainViewModel.HoverThumbnailPosition):
                _hoverWindow?.Position = vm.HoverThumbnailPosition;
                break;
        }
    }

    private void OnHoverThumbnailVisibilityChanged(bool isVisible)
    {
        if (isVisible)
        {
            if (_hoverWindow == null)
            {
                _hoverWindow = new HoverThumbnailWindow();
                _hoverWindow.Show();
            }
            else
            {
                _hoverWindow.Show();
            }
        }
        else
        {
            _hoverWindow?.Hide();
        }
    }

    public void CloseHoverWindow()
    {
        _hoverWindow?.Close();
        _hoverWindow = null;
    }

    private void OnMainItemImageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image image) return;

        var userPreferences = UserPreferencesService.Instance.Repository.Settings;
        var bitmapInterpolationMode = userPreferences.AntiAliasingMode.GetInterpolationMode();
        if (bitmapInterpolationMode != BitmapInterpolationMode.None && bitmapInterpolationMode != BitmapInterpolationMode.Unspecified)
            RenderOptions.SetBitmapInterpolationMode(image, bitmapInterpolationMode);

        image.PointerEntered += OnMainItemImagePointerEntered;
        image.PointerExited += OnMainItemImagePointerExited;
        image.PointerMoved += OnMainItemImagePointerMoved;
    }

    private void OnMainItemImageUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image image) return;
        image.PointerEntered -= OnMainItemImagePointerEntered;
        image.PointerExited -= OnMainItemImagePointerExited;
        image.PointerMoved -= OnMainItemImagePointerMoved;
    }

    private void OnMainItemButtonLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        button.AddHandler(PointerPressedEvent, OnMainItemButtonPointerPressed, RoutingStrategies.Tunnel);
    }
    private void OnMainItemButtonUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        button.RemoveHandler(PointerPressedEvent, OnMainItemButtonPointerPressed);
    }

    private void OnMainItemImagePointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Image image || DataContext is not MainViewModel vm) return;
        if (image.DataContext is not ItemViewModel item) return;

        vm.UpdateHoverThumbnailPosition(GetScreenPosition(e));
        vm.ShowHoverThumbnail(item);
    }

    private void OnMainItemImagePointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.HideHoverThumbnail();
    }

    private void OnMainItemImagePointerMoved(object? sender, PointerEventArgs e)
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

    private async void OnMainItemButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ItemViewModel item) return;

        var viewModelType = item.ViewModelType;
        if (viewModelType != ViewModelType.Item && viewModelType != ViewModelType.File && viewModelType != ViewModelType.Folder) return;

        var transferItem = new DataTransferItem();
        string? droppedPath = null;

        if (viewModelType == ViewModelType.File && !string.IsNullOrEmpty(item.ActualValue))
        {
            var storageFile = await StorageService.GetStorageFileFromPath(TopLevelProvider.Current, item.ActualValue);
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

        MainWindowViewModel.Instance.LastDragDropPath = droppedPath;
        await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Copy);
    }
}
