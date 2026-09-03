using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public enum ItemContentType
{
    Unknown,
    File,
    Folder,
    URL
}

public partial class ItemContentViewModel : ViewModelBase
{
    [Reactive] public partial Bitmap? IconImage { get; set; } = null;
    [Reactive] public partial string FileName { get; set; }
    [Reactive] public partial string FullPath { get; set; }
    [Reactive] public partial bool IsUrl { get; set; } = false;

    public ItemContentViewModel(string fileName, string path, ItemContentType type)
    {
        FileName = fileName;
        FullPath = path;

        if (type == ItemContentType.Unknown)
        {
            IconImage = ImageService.Peek(SystemIconKey.UnknownFileIcon);
        }
        else if (type == ItemContentType.URL)
        {
            IconImage = ImageService.Peek(SystemIconKey.LinkIcon);
            IsUrl = true;
        }
        else if (type == ItemContentType.File)
        {
            IconImage = ImageService.Peek(SystemIconKey.FileIcon);
        }
        else if (type == ItemContentType.Folder)
        {
            IconImage = ImageService.Peek(SystemIconKey.FolderIcon);
        }
    }
}
