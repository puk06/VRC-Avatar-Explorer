using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class ItemViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; } = true;
    [Reactive] public bool IsSelected { get; set; } = false;

    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string Title { get; private set; } = string.Empty;
    [Reactive] public string Description { get; private set; } = string.Empty;
    [Reactive] public IEnumerable<TagViewModel> Tags { get; set; } = [];
    [Reactive] public ContextMenu? ContextMenu { get; set; } = null;
    [Reactive] public string ToolTip { get; set; } = string.Empty;

    [Reactive] public double Width { get; set; } = 0;
    [Reactive] public double Height { get; set; } = 0;

    public string ImageFileName { get; set; } = string.Empty;
    public string TitleRaw { get; set; } = string.Empty;
    public bool TitleLocalizable { get; set; } = false;

    public LoclizableField DescriptionRaw = new();

    public ContextMenuAction[] Actions { get; set; } = [];
    public Action<ContextMenuAction>? onMenuClick = null;

    public string Identifier { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public required ViewModelType ViewModelType { get; set; }

    public ItemViewModel Update()
    {
        Thumbnail = ImageService.Get(ImageFileName);
        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;
        Description = DescriptionRaw.Args == null ? Localizer.Instance[DescriptionRaw.Key] : Localizer.Instance.Get(DescriptionRaw.Key, DescriptionRaw.Args);
        ContextMenu = ContextMenuFactory.GetContextMenu(Actions, HandleMenuClick);

        Width = Height = (Thumbnail != null) ? 80 : 0;
        return this;
    }

    private void HandleMenuClick(ContextMenuAction action)
    {
        ContextMenuHandlerService.Handle(action.ActionKey, Identifier);
    }
}
