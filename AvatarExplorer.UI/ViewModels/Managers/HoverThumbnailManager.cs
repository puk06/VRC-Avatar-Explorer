using Avalonia;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class HoverThumbnailManager(MainViewModel vm)
{
    private readonly MainViewModel _vm = vm;

    public void Show(ItemViewModel item)
    {
        var isSuppotedType = item.ViewModelType == ViewModelType.Item || item.ViewModelType == ViewModelType.Avatar || (item.ViewModelType == ViewModelType.File && item.ThumbnailFilePath != null);
        if (!isSuppotedType || item.Thumbnail == null) return;

        if (InstanceRepository.UserPreferences.Settings.EnableHoverIconSize)
        {
            _vm.HoverThumbnailImage = item.Thumbnail;
            _vm.IsHoverThumbnailVisible = true;
        }

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
