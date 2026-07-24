using System.IO;
using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public enum ItemPathType
{
    Unknown,
    File,
    Folder
}

public class ItemPathViewModel : ViewModelBase
{
    [Reactive] public Bitmap? IconImage { get; set; } = null;
    [Reactive] public string FileName { get; set; } = string.Empty;
    [Reactive] public string FullPath { get; set; } = string.Empty;

    public ItemPathViewModel(string path, ItemPathType type)
    {
        FileName = Path.GetFileName(path);
        FullPath = path;

        if (type == ItemPathType.Unknown) IconImage = ImageService.Get(SystemIconKey.UnknownFileIcon);
        else if (type == ItemPathType.File) IconImage = ImageService.Get(SystemIconKey.FileIcon);
        else if (type == ItemPathType.Folder) IconImage = ImageService.Get(SystemIconKey.FolderIcon);
    }
}
