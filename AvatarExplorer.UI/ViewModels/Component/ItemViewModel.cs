using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class ItemViewModel : ViewModelBase
{
    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string Title { get; private set; } = string.Empty;
    [Reactive] public string Description { get; private set; } = string.Empty;
    [Reactive] public IEnumerable<TagViewModel> Tags { get; set; } = [];
    [Reactive] public ContextMenu? ContextMenu { get; set; } = null;

    public string ImageFileName { get; set; } = string.Empty;
    public IconType IconType { get; set; } = IconType.None;
    public string TitleRaw { get; set; } = string.Empty;
    public bool TitleLocalizable { get; set; } = false;
    public LoclizableDescription DescriptionRaw = new();
    [Reactive] public string ToolTip { get; set; } = string.Empty;
    public ContextMenuAction[] Actions { get; private set; } = [];

    public string Tag { get; set; } = string.Empty;

    [Reactive] public double Width { get; set; } = 80;
    [Reactive] public double Height { get; set; } = 80;

    public ItemViewModel Update()
    {
        Thumbnail = ImageService.Get(ImageFileName, IconType);
        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;
        Description = Localizer.Instance.Get(DescriptionRaw.LocalizationKey, DescriptionRaw.Args);
        ContextMenu = ContextMenuFactory.GetContextMenu(Actions);
        return this;
    }
}
