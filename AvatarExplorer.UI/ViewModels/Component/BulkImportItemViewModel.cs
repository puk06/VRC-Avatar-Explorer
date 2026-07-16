using System.Collections.Generic;
using Avalonia.Media.Imaging;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class BulkImportItemViewModel : ViewModelBase
{
    // private readonly UISelectableItem _item;

    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Description { get; set; } = string.Empty;

    [Reactive] public IEnumerable<string> UnitypackageNames { get; set; } = [];
    public IEnumerable<string> UnitypackageFullPaths { get; set; } = [];
    [Reactive] public int SelectedUnitypackage { get; set; } = 0;

    [Reactive] public string ToolTip { get; set; } = string.Empty;

    [Reactive] public double Width { get; set; } = 0;
    [Reactive] public double Height { get; set; } = 0;
    [Reactive] public BitmapInterpolationMode BitmapInterpolationMode { get; set; } = BitmapInterpolationMode.None;

    // public BulkImportItemViewModel(UISelectableItem item, string? fileName = null)
    // {
    //     _item = item;

    //     Thumbnail = ImageService.Get(_item.ImageFileName, _item.IconType);
        
    //     Title = _item.Title;

    //     if (_item.ItemFolderPaths != null)
    //     {
    //         UnitypackageFullPaths = UnitypackageService.GetUnitypackagePaths(_item.ItemFolderPaths);
    //         UnitypackageNames = UnitypackageFullPaths
    //             .Select(i => Path.GetFileName(i) ?? string.Empty)
    //             .Where(i => !string.IsNullOrEmpty(i));

    //         if (fileName != null)
    //         {
    //             int index = UnitypackageFullPaths.IndexOf(fileName);
    //             if (index != -1) SelectedUnitypackage = index;
    //         }
    //     }

    //     UpdateLocalization();
    // }

    public BulkImportItemViewModel Copy() => new();

    // public void UpdateLocalization()
    // {
    //     Description = Localizer.Instance.Get(_item.Description.LocalizationKey, _item.Description.Args);
    //     ToolTip = _item.GetToolTipText();
    // }
}
