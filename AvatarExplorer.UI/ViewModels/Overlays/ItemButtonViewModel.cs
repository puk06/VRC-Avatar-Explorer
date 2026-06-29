using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ItemButtonViewModel : ViewModelBase
{
    private readonly UISelectableItem _item;
    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string ImageFileName { get; set; } = string.Empty;
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Description { get; set; } = string.Empty;
    [Reactive] public IEnumerable<TagViewModel> Tags { get; set; } = [];
    [Reactive] public string ToolTip { get; set; } = string.Empty;
    
    [Reactive] public ContextMenu? ContextMenu { get; set; } = null;
    public ContextMenuAction[] Actions { get; set; } = [];

    public ItemTagInfo TagInfo { get; set; }  = new(ItemTagStates.None, string.Empty); // ボタンが選択されたときに使用されるタグ

    [Reactive] public double Width { get; set; } = 0;
    [Reactive] public double Height { get; set; } = 0;

    public ItemButtonViewModel(UISelectableItem item, ContextMenuAction[]? actions = null)
    {
        _item = item;

        Thumbnail = ImageService.Get(_item.ImageFileName, _item.IconType);
        ImageFileName = _item.ImageFileName;
        Title = _item.Title;
        TagInfo = item.Tag;

        UpdateLocalization();
    }

    public ItemButtonViewModel Copy() => new(_item);

    public void UpdateLocalization()
    {
        ContextMenu = ContextMenuFactory.GetContextMenu(Actions);
        Description = Localizer.Instance.Get(_item.Description.LocalizationKey, _item.Description.Args);
        ToolTip = _item.GetToolTipText();
    }
}
