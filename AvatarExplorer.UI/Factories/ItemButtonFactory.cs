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
        var itemButton = CreateBaseButton(item);

        var contentGrid = new Grid() { ColumnSpacing = 10, ColumnDefinitions = new("Auto,*") };

        // アイコン (アイコンにCornerRadiusを適用するため、ChildにImageが指定されたBorderが返ってくる)
        var itemIconBorder = CreateItemIconBorder(item, userPreferences);
        if (itemIconBorder != null)
        {
            contentGrid.Children.Add(itemIconBorder);
            Grid.SetColumn(itemIconBorder, 0);
        }

        // テキスト + タグ部分
        var textGrid = CreateTextAndTagGrid(item, runtimeSettings);
        contentGrid.Children.Add(textGrid);
        Grid.SetColumn(textGrid, 1);

        itemButton.Content = contentGrid;
        SetupButtonInteractions(itemButton, item, runtimeSettings, contextMenu, onClick);

        parent.Children.Add(itemButton);
        return itemButton;
    }

    internal static Button CreateBaseButton(UISelectableItem item)
    {
        var button = new Button() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top, Tag = item.Tag, CornerRadius = new(8), Padding = new(7) };
        button.Classes.Add(ButtonClass);
        return button;
    }

    internal static Border? CreateItemIconBorder(UISelectableItem item, UserPreferences userPreferences, bool enableHoverIconSize = true)
    {
        var itemIconBitmap = ImageService.Get(item.ImageFileName, item.IconType);
        var isFallbackIcon = itemIconBitmap == null;

        if (isFallbackIcon && item.ImageFileName == SystemIconKey.None) return null; // アイコンがなく、アイコンタイプも指定されていない場合はアイコンなし

        var iconBorder = new Border() { CornerRadius = new(8), ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };

        var itemIcon = new Image()
        {
            Source = itemIconBitmap ?? ImageService.Get(SystemIconKey.FileIcon),
            Width = userPreferences.NormalIconSize,
            Height = userPreferences.NormalIconSize,
            Stretch = Stretch.Fill,
            VerticalAlignment = VerticalAlignment.Top
        };
        iconBorder.Child = itemIcon;

        iconBorder.Width = itemIcon.Width;
        iconBorder.Height = itemIcon.Height;

        var bitmapInterpolationMode = userPreferences.AntiAliasingMode.GetInterpolationMode();
        if (bitmapInterpolationMode != BitmapInterpolationMode.None && bitmapInterpolationMode != BitmapInterpolationMode.Unspecified) RenderOptions.SetBitmapInterpolationMode(itemIcon, bitmapInterpolationMode);

        if (!ImageService.IsSystemIcon(item.ImageFileName) && !isFallbackIcon && userPreferences.EnableHoverIconSize && enableHoverIconSize)
        {
            itemIcon.PointerEntered += (s, e) =>
            {
                itemIcon.Width = userPreferences.HoverIconSize;
                itemIcon.Height = double.NaN;

                iconBorder.Width = itemIcon.Width;
                iconBorder.Height = itemIcon.Height;
            };

            itemIcon.PointerExited += (s, e) =>
            {
                itemIcon.Width = userPreferences.NormalIconSize;
                itemIcon.Height = userPreferences.NormalIconSize;

                iconBorder.Width = itemIcon.Width;
                iconBorder.Height = itemIcon.Height;
            };
        }

        return iconBorder;
    }

    internal static Grid CreateTextAndTagGrid(UISelectableItem item, RuntimeSettings runtimeSettings)
    {
        var textGrid = new Grid() { RowDefinitions = new("Auto,Auto,5,*") };

        var itemTitle = GetFormattedTitle(item, runtimeSettings);

        var titleTextBlock = new TextBlock() { Text = itemTitle, FontSize = 16, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(titleTextBlock, 0);
        textGrid.Children.Add(titleTextBlock);

        var descriptionTextBlock = new TextBlock() { Text = Localizer.Instance.Get(item.Description.LocalizationKey, item.Description.Args), FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(descriptionTextBlock, 1);
        textGrid.Children.Add(descriptionTextBlock);

        var tagPanel = CreateTagPanel(item);
        Grid.SetRow(tagPanel, 3);
        textGrid.Children.Add(tagPanel);

        return textGrid;
    }

    internal static string GetFormattedTitle(UISelectableItem item, RuntimeSettings runtimeSettings)
    {
        var title = StateFlagUtils.IsCategoryState(item.Tag.State) ? Localizer.Instance[item.Title] : item.Title;

        // アイテムの場合は設定をチェックして括弧を削除してあげる
        if (runtimeSettings.RemoveBrackets && StateFlagUtils.IsItemState(item.Tag.State))
        {
            title = ItemUtils.RemoveBrackets(title);
        }

        return title;
    }

    internal static WrapPanel CreateTagPanel(UISelectableItem item)
    {
        var tagPanel = new WrapPanel() { Orientation = Orientation.Horizontal, ItemSpacing = 5, LineSpacing = 5 };

        if (!string.IsNullOrEmpty(item.CommonAvatarName))
        {
            var commonAvatarBorder = GetTagBorder(Localizer.Instance.Get(LocalizationKey.Button.Tag.CommonAvatar, item.CommonAvatarName));
            if (commonAvatarBorder.Child is TextBlock tagLabel)
            {
                tagLabel.FontWeight = FontWeight.Bold;
                tagLabel.Classes.Add("commonavatar");
            }

            commonAvatarBorder.Classes.Add("commonavatar");
            tagPanel.Children.Add(commonAvatarBorder);
        }

        foreach (var itemTag in item.ItemTags)
        {
            var tagBorder = GetTagBorder(itemTag);
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
            // ToolTip.SetTip(button, item.IsTempAvatar ? item.Title : GetTooltipTextFromItem(item));
            ToolTip.SetShowDelay(button, 1500);
            ToolTip.SetBetweenShowDelay(button, -1);
        }
        else if (item.Tag.State == ItemTagStates.ItemFileCategoryOpen)
        {
            ToolTip.SetTip(button, Localizer.Instance.Get(LocalizationKey.Button.ToolTip.FilePath, Path.GetRelativePath(runtimeSettings.DataRootDirectory, item.Tag.Value)));
            ToolTip.SetShowDelay(button, 1500);
            ToolTip.SetBetweenShowDelay(button, -1);
        }

        if (contextMenu != null && contextMenu.ItemCount > 0) button.ContextMenu = contextMenu;
        if (onClick != null) button.Click += onClick;
    }

    internal static Border GetTagBorder(string text)
    {
        var tagButton = new Border() { CornerRadius = new(15), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var tagLabel = new TextBlock() { Text = text, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new(8, 5) };
        tagButton.Child = tagLabel;

        return tagButton;
    }
}
