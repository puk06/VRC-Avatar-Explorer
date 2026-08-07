using Avalonia;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class HoverThumbnailManager(MainViewModel vm)
{
    private readonly MainViewModel _vm = vm;

    public void Show(ItemViewModel item)
    {
        if (item.ViewModelType != ViewModelType.Item || item.Thumbnail == null) return;

        var preferences = UserPreferencesService.Instance.Repository.Settings;
        if (!preferences.EnableHoverIconSize) return;

        _vm.HoverThumbnailImage = item.Thumbnail;
        _vm.IsHoverThumbnailVisible = true;
    }

    public void Hide()
    {
        _vm.IsHoverThumbnailVisible = false;
    }

    public void UpdatePosition(PixelPoint position)
    {
        _vm.HoverThumbnailPosition = position;
    }
}
