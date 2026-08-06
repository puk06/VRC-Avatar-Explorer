using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.Utils;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class ItemViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; } = true;
    [Reactive] public bool IsSelected { get; set; } = false;
    [Reactive] public bool IsImplemented { get; set; } = false;
    [Reactive] public bool IsNotImplemented { get; set; } = false;

    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string Title { get; private set; } = string.Empty;
    [Reactive] public string Description { get; private set; } = string.Empty;
    [Reactive] public TagViewModel[] Tags { get; set; } = [];
    [Reactive] public ContextMenu? ContextMenu { get; set; } = null;
    [Reactive] public string? ToolTip { get; set; } = null;

    [Reactive] public double Width { get; set; } = 0;
    [Reactive] public double Height { get; set; } = 0;

    public string ImageFileName { get; set; } = string.Empty;
    public string TitleRaw { get; set; } = string.Empty;
    public bool TitleLocalizable { get; set; } = false;

    public LoclizableField DescriptionRaw = new();

    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public string ItemMemo { get; set; } = string.Empty;

    public ContextMenuAction[] Actions { get; set; } = [];
    public Action<ContextMenuAction>? onMenuClick = null;

    public string Identifier { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public required ViewModelType ViewModelType { get; set; }

    public ItemViewModel Update(int iconSize = 80, bool removeBrackets = false)
    {
        Thumbnail = ImageService.Get(ImageFileName);
        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;
        Description = DescriptionRaw.Args == null ? Localizer.Instance[DescriptionRaw.Key] : Localizer.Instance.Get(DescriptionRaw.Key, DescriptionRaw.Args);
        ContextMenu = ContextMenuFactory.GetContextMenu(Actions, HandleMenuClick);

        Width = Height = (Thumbnail != null) ? iconSize : 0;
        Tags.ForEach(i => i.Update());

        if (removeBrackets && ViewModelType == ViewModelType.Item)
        {
            Title = TextBracketsUtils.RemoveBrackets(TitleRaw);
        }

        ToolTip = GenerateToolTipText();

        return this;
    }

    private string? GenerateToolTipText()
    {
        if (ViewModelType == ViewModelType.Item)
        {
            var sb = new StringBuilder();
            sb.Append(TitleRaw);

            if (!string.IsNullOrEmpty(CreatedDate) || !string.IsNullOrEmpty(UpdatedDate))
            {
                sb.AppendLine();
                sb.AppendLine();

                if (!string.IsNullOrEmpty(CreatedDate))
                    sb.AppendLine(
                        Localizer.Instance.Get(
                            Loc.Button.ToolTip.CreatedDate,
                            DatetimeUtils.GetDateStringFromUnixTime(CreatedDate)
                        )
                    );

                if (!string.IsNullOrEmpty(CreatedDate))
                    sb.Append(
                        Localizer.Instance.Get(
                            Loc.Button.ToolTip.UpdatedDate,
                            DatetimeUtils.GetDateStringFromUnixTime(UpdatedDate)
                        )
                    );
            }

            if (!string.IsNullOrEmpty(ItemMemo))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(ItemMemo);
            }

            return sb.ToString();
        }

        if (ViewModelType == ViewModelType.TempAvatar)
        {
            return TitleRaw;
        }

        if (ViewModelType == ViewModelType.File)
        {
            return Localizer.Instance.Get(Loc.Button.ToolTip.FilePath, ActualValue ?? string.Empty);
        }

        return null;
    }

    private void HandleMenuClick(ContextMenuAction action)
    {
        ContextMenuHandlerService.Handle(action.ActionKey, ActualValue ?? Identifier);
    }
}
