using System;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI.Factories;

internal static class ItemButtonFactory
{
    private const string ButtonClass = "button";

    internal static Button AddItemButton(StackPanel parent, UISelectableItem item, RuntimeSettings runtimeSettings, UserPreferences userPreferences, ContextMenu? contextMenu = null, EventHandler<RoutedEventArgs>? onClick = null)
    {
        Button itemButton = CreateBaseButton(item);

        Grid contentGrid = new() { ColumnSpacing = 10, ColumnDefinitions = new("Auto,*") };

        // アイコン (アイコンにCornerRadiusを適用するため、ChildにImageが指定されたBorderが返ってくる)
        Border itemIconBorder = CreateItemIconBorder(item, userPreferences);
        contentGrid.Children.Add(itemIconBorder);
        Grid.SetColumn(itemIconBorder, 0);

        // テキスト + タグ部分
        Grid textGrid = CreateTextAndTagGrid(item, runtimeSettings);
        contentGrid.Children.Add(textGrid);
        Grid.SetColumn(textGrid, 1);

        itemButton.Content = contentGrid;
        SetupButtonInteractions(itemButton, item, runtimeSettings, contextMenu, onClick);

        parent.Children.Add(itemButton);
        return itemButton;
    }

    internal static Button CreateBaseButton(UISelectableItem item)
    {
        Button button = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top, Tag = item.Tag, CornerRadius = new(8), Padding = new(7) };
        button.Classes.Add(ButtonClass);
        return button;
    }

    internal static Border CreateItemIconBorder(UISelectableItem item, UserPreferences userPreferences, bool enableHoverIconSize = true)
    {
        Bitmap? itemIconBitmap = ImageService.Get(item.ImageFileName, item.IconType);
        bool isFallbackIcon = itemIconBitmap == null;

        Image itemIcon = new()
        {
            Source = itemIconBitmap ?? ImageService.Get(SystemIconKey.FileIcon),
            Width = userPreferences.NormalIconSize,
            Height = userPreferences.NormalIconSize,
            Stretch = Stretch.Fill,
            VerticalAlignment = VerticalAlignment.Top
        };
        BitmapInterpolationMode bitmapInterpolationMode = userPreferences.AntiAliasingMode.GetInterpolationMode();
        if (bitmapInterpolationMode != BitmapInterpolationMode.None && bitmapInterpolationMode != BitmapInterpolationMode.Unspecified) RenderOptions.SetBitmapInterpolationMode(itemIcon, bitmapInterpolationMode);

        if (!ImageService.IsSystemIcon(item.ImageFileName) && !isFallbackIcon && userPreferences.EnableHoverIconSize && enableHoverIconSize)
        {
            itemIcon.PointerEntered += (s, e) =>
            {
                itemIcon.Width = userPreferences.HoverIconSize;
                itemIcon.Height = double.NaN;
            };

            itemIcon.PointerExited += (s, e) =>
            {
                itemIcon.Width = userPreferences.NormalIconSize;
                itemIcon.Height = userPreferences.NormalIconSize;
            };
        }

        return new() { ClipToBounds = true, CornerRadius = new(8), Child = itemIcon };
    }

    internal static Grid CreateTextAndTagGrid(UISelectableItem item, RuntimeSettings runtimeSettings)
    {
        Grid textGrid = new() { RowDefinitions = new("Auto,Auto,5,*") };

        string itemTitle = GetFormattedTitle(item, runtimeSettings);

        TextBlock titleTextBlock = new() { Text = itemTitle, FontSize = 16, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(titleTextBlock, 0);
        textGrid.Children.Add(titleTextBlock);

        TextBlock descriptionTextBlock = new() { Text = Localizer.Instance.Get(item.Description.LocalizationKey, item.Description.Args), FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(descriptionTextBlock, 1);
        textGrid.Children.Add(descriptionTextBlock);

        WrapPanel tagPanel = CreateTagPanel(item);
        Grid.SetRow(tagPanel, 3);
        textGrid.Children.Add(tagPanel);

        return textGrid;
    }

    internal static string GetFormattedTitle(UISelectableItem item, RuntimeSettings runtimeSettings)
    {
        string title = StateFlagUtils.IsCategoryState(item.Tag.State) ? Localizer.Instance[item.Title] : item.Title;

        // アイテムの場合は設定をチェックして括弧を削除してあげる
        if (runtimeSettings.RemoveBrackets && StateFlagUtils.IsItemState(item.Tag.State))
        {
            title = ItemUtils.RemoveBrackets(title);
        }

        return title;
    }

    internal static WrapPanel CreateTagPanel(UISelectableItem item)
    {
        WrapPanel tagPanel = new() { Orientation = Orientation.Horizontal, ItemSpacing = 5, LineSpacing = 5 };

        if (!string.IsNullOrEmpty(item.CommonAvatarName))
        {
            Border commonAvatarBorder = GetTagBorder(Localizer.Instance.Get(LocalizationKey.Button.Tag.CommonAvatar, item.CommonAvatarName));
            if (commonAvatarBorder.Child is TextBlock tagLabel)
            {
                tagLabel.FontWeight = FontWeight.Bold;
                tagLabel.Classes.Add("commonavatar");
            }

            commonAvatarBorder.Classes.Add("commonavatar");
            tagPanel.Children.Add(commonAvatarBorder);
        }

        foreach (string itemTag in item.ItemTagsView)
        {
            Border tagBorder = GetTagBorder(itemTag);
            if (tagBorder.Child is TextBlock tagLabel)
            {
                tagLabel.FontWeight = FontWeight.Bold;
                tagLabel.Classes.Add("tag");
            }

            tagBorder.Classes.Add("tag");

            tagPanel.Children.Add(tagBorder);
        }

        return tagPanel;
    }

    internal static void SetupButtonInteractions(Button button, UISelectableItem item, RuntimeSettings runtimeSettings, ContextMenu? contextMenu, EventHandler<RoutedEventArgs>? onClick)
    {
        if (StateFlagUtils.IsItemState(item.Tag.State))
        {
            ToolTip.SetTip(button, item.IsTempAvatar ? item.Title : GetTooltipTextFromItem(item));
            ToolTip.SetBetweenShowDelay(button, -1);
        }
        else if (item.Tag.State == ItemTagStates.ItemFileCategoryOpen)
        {
            ToolTip.SetTip(button, Localizer.Instance.Get(LocalizationKey.Button.ToolTip.FilePath, Path.GetRelativePath(runtimeSettings.DataRootDirectory, item.Tag.Value)));
            ToolTip.SetBetweenShowDelay(button, -1);
        }

        if (contextMenu != null && contextMenu.ItemCount > 0) button.ContextMenu = contextMenu;
        if (onClick != null) button.Click += onClick;
    }

    internal static Border GetTagBorder(string text)
    {
        Border tagButton = new() { CornerRadius = new(15), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        TextBlock tagLabel = new() { Text = text, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new(8, 5) };
        tagButton.Child = tagLabel;

        return tagButton;
    }

    internal static string? GetTooltipTextFromItem(UISelectableItem item)
    {
        StringBuilder toolTipTextBuilder = new();

        toolTipTextBuilder.Append(item.Title);

        toolTipTextBuilder.AppendLine();
        toolTipTextBuilder.AppendLine();

        toolTipTextBuilder.Append(Localizer.Instance.Get(LocalizationKey.Button.ToolTip.CreatedDate, item.CreatedDate));
        toolTipTextBuilder.AppendLine();
        toolTipTextBuilder.Append(Localizer.Instance.Get(LocalizationKey.Button.ToolTip.UpdatedDate, item.UpdatedDate));

        if (!string.IsNullOrEmpty(item.ItemMemo))
        {
            toolTipTextBuilder.AppendLine();
            toolTipTextBuilder.AppendLine();

            toolTipTextBuilder.Append(item.ItemMemo);
        }

        return toolTipTextBuilder.ToString();
    }
}
