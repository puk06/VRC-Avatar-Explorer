using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.External;
using Material.Icons;
using Material.Icons.Avalonia;

namespace AvatarExplorer.UI.Factories;

internal static class UnitypackageSelectorButtonFactory
{
    internal static Button AddItemButton(
        StackPanel parent,
        UISelectableItem item,
        RuntimeSettings runtimeSettings, UserPreferences userPreferences,
        int itemIndex, int selectedIndex,
        Action<int>? onCopyClick = null, Action<int>? onRemoveClick = null, Action<int, int>? onSelectionChanged = null
    )
    {
        Button itemButton = ItemButtonFactory.CreateBaseButton(item);

        StackPanel contentStackPanel = new() { Spacing = 5 };

        Grid contentGrid = new() { ColumnSpacing = 10, ColumnDefinitions = new("Auto,*") };

        // アイコン (アイコンにCornerRadiusを適用するため、ChildにImageが指定されたBorderが返ってくる)
        Border itemIconBorder = ItemButtonFactory.CreateItemIconBorder(item, userPreferences, false);
        contentGrid.Children.Add(itemIconBorder);
        Grid.SetColumn(itemIconBorder, 0);

        // タイトルリスト
        Grid textGrid = CreateTextAndIconGrid(item, runtimeSettings, itemIndex, onCopyClick, onRemoveClick);
        contentGrid.Children.Add(textGrid);
        Grid.SetColumn(textGrid, 1);

        contentStackPanel.Children.Add(contentGrid);

        Panel unitypackagePanel = new();
        unitypackagePanel.Children.Add(CreateUnitypackageList(item, runtimeSettings, itemIndex, selectedIndex, onSelectionChanged));

        contentStackPanel.Children.Add(unitypackagePanel);

        itemButton.Content = contentStackPanel;
        ItemButtonFactory.SetupButtonInteractions(itemButton, item, runtimeSettings, null, null);

        parent.Children.Add(itemButton);
        return itemButton;
    }

    internal static Grid CreateTextAndIconGrid(UISelectableItem item, RuntimeSettings runtimeSettings, int itemIndex, Action<int>? onCopyClick = null, Action<int>? onRemoveClick = null)
    {
        Grid textGrid = new() { RowDefinitions = new("Auto,Auto,5,*") };

        string itemTitle = ItemButtonFactory.GetFormattedTitle(item, runtimeSettings);

        TextBlock titleTextBlock = new() { Text = itemTitle, FontSize = 16, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(titleTextBlock, 0);
        textGrid.Children.Add(titleTextBlock);

        TextBlock descriptionTextBlock = new() { Text = Localizer.Instance.Get(item.Description.LocalizationKey, item.Description.Args), FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(descriptionTextBlock, 1);
        textGrid.Children.Add(descriptionTextBlock);

        Panel iconPanel = new();
        StackPanel iconStackPanel = new() { Orientation = Orientation.Horizontal, Spacing = 5 };

        Button copyButton = new()
        {
            Content = new MaterialIcon()
            {
                Kind = MaterialIconKind.ContentCopy,
                Width = 20,
                Height = 20
            },
            Background = new SolidColorBrush()
            {
                Opacity = 0
            }
        };
        if (onCopyClick != null) copyButton.Click += (_, e) => onCopyClick(itemIndex);

        iconStackPanel.Children.Add(copyButton);

        Button removeButton = new Button()
        {
            Content = new MaterialIcon()
            {
                Kind = MaterialIconKind.TrashCan,
                Width = 20,
                Height = 20
            },
            Foreground = new SolidColorBrush(Color.FromRgb(210, 0, 0)),
            Background = new SolidColorBrush()
            {
                Opacity = 0
            }
        };
        if (onRemoveClick != null) removeButton.Click += (_, e) => onRemoveClick(itemIndex);

        iconStackPanel.Children.Add(removeButton);

        iconPanel.Children.Add(iconStackPanel);
        textGrid.Children.Add(iconPanel);
        Grid.SetRow(iconPanel, 3);

        return textGrid;
    }

    internal static ComboBox CreateUnitypackageList(UISelectableItem item, RuntimeSettings runtimeSettings, int itemIndex, int selectedIndex, Action<int, int>? onSelectedIndexChanged = null)
    {
        ComboBox unitypackageComboBox = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, CornerRadius = new(8), FontSize = 14 };

        foreach (string filePath in UnitypackageService.GetUnitypackagePaths(ItemUtils.GetItemPath(runtimeSettings.DataRootDirectory, item.ItemPath)))
        {
            ComboBoxItem unitypackageFileItem = new()
            {
                Content = Path.GetFileName(filePath),
                Tag = filePath
            };

            unitypackageComboBox.Items.Add(unitypackageFileItem);
            ToolTip.SetTip(unitypackageFileItem, Path.GetFileName(Path.GetDirectoryName(filePath)) + " > " + Path.GetFileName(filePath));
            ToolTip.SetBetweenShowDelay(unitypackageFileItem, -1);
        }

        if (onSelectedIndexChanged != null) unitypackageComboBox.SelectionChanged += (s, e) => onSelectedIndexChanged(itemIndex, unitypackageComboBox.SelectedIndex);
        unitypackageComboBox.SelectedIndex = selectedIndex;

        return unitypackageComboBox;
    }
}
