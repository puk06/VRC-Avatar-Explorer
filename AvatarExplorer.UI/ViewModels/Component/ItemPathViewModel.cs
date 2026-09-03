using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public enum ItemPathType
{
    Unknown,
    File,
    Folder,
    URL
}

public partial class ItemPathViewModel : ViewModelBase
{
    [Reactive] public partial Bitmap? IconImage { get; set; } = null;
    [Reactive] public partial string FileName { get; set; }
    [Reactive] public partial string FullPath { get; set; }
    [Reactive] public partial bool IsUrl { get; set; } = false;

    public ItemPathViewModel(string fileName, string path, ItemPathType type)
    {
        FileName = fileName;
        FullPath = path;

        if (type == ItemPathType.Unknown)
        {
            IconImage = ImageService.Peek(SystemIconKey.UnknownFileIcon);
        }
        else if (type == ItemPathType.URL)
        {
            IconImage = ImageService.Peek(SystemIconKey.LinkIcon);
            IsUrl = true;
        }
        else if (type == ItemPathType.File)
        {
            IconImage = ImageService.Peek(SystemIconKey.FileIcon);
        }
        else if (type == ItemPathType.Folder)
        {
            IconImage = ImageService.Peek(SystemIconKey.FolderIcon);
        }
    }
}
