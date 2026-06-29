using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.UI.ViewModels.Component;

public class ItemViewModel : ViewModelBase
{
    public Bitmap? Thumbnail { get; set; } = null;
    public string ImageFileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<TagViewModel> Tags { get; set; } = [];
    public string ToolTip { get; set; } = string.Empty;
    public ContextMenu? ContextMenu { get; set; } = null;
    public required ItemTagInfo State { get; set; }

    public double Width { get; set; } = 0;
    public double Height { get; set; } = 0;
    public BitmapInterpolationMode BitmapInterpolationMode { get; set; } = BitmapInterpolationMode.None;
}
