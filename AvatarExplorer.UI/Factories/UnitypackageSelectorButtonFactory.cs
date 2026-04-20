using System;
using System.Collections.Immutable;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.External;
using Material.Icons;
using Material.Icons.Avalonia;

namespace AvatarExplorer.UI.Factories;

internal static class UnitypackageSelectorButtonFactory
{
    internal static Button AddItemButton(UnitypackageSelectorButtonOptions options)
    {
        Button itemButton = ItemButtonFactory.CreateBaseButton(options.Item);

        StackPanel contentStackPanel = new() { Spacing = 5 };

        Grid contentGrid = new() { ColumnSpacing = 10, ColumnDefinitions = new("Auto,*") };

        // アイコン (アイコンにCornerRadiusを適用するため、ChildにImageが指定されたBorderが返ってくる)
        Border? itemIconBorder = ItemButtonFactory.CreateItemIconBorder(options.Item, options.UserPreferences, false);
        if (itemIconBorder != null)
        {
            contentGrid.Children.Add(itemIconBorder);
            Grid.SetColumn(itemIconBorder, 0);
        }

        // タイトルリスト
        Grid textGrid = CreateTextAndIconGrid(options.Item, options.RuntimeSettings, options.Id, options.OnCopyClick, options.OnRemoveClick);
        contentGrid.Children.Add(textGrid);
        Grid.SetColumn(textGrid, 1);

        contentStackPanel.Children.Add(contentGrid);

        Panel unitypackagePanel = new();
        unitypackagePanel.Children.Add(CreateUnitypackageList(options.Item, options.Id, options.SelectedFilePath, options.RuntimeSettings, options.OnSelectionChanged));

        contentStackPanel.Children.Add(unitypackagePanel);

        itemButton.Content = contentStackPanel;
        ItemButtonFactory.SetupButtonInteractions(itemButton, options.Item, options.RuntimeSettings, null, null);

        options.Parent.Children.Add(itemButton);
        return itemButton;
    }

    internal static Grid CreateTextAndIconGrid(UISelectableItem item, RuntimeSettings runtimeSettings, string id, Action<string>? onCopyClick = null, Action<string>? onRemoveClick = null)
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
        if (onCopyClick != null) copyButton.Click += (_, e) => onCopyClick(id);

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
        if (onRemoveClick != null) removeButton.Click += (_, e) => onRemoveClick(id);

        iconStackPanel.Children.Add(removeButton);

        iconPanel.Children.Add(iconStackPanel);
        textGrid.Children.Add(iconPanel);
        Grid.SetRow(iconPanel, 3);

        return textGrid;
    }

    internal static ComboBox CreateUnitypackageList(UISelectableItem item, string id, string selectedFilePath, RuntimeSettings runtimeSettings, Action<string, string>? onSelectedIndexChanged = null)
    {
        ComboBox unitypackageComboBox = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, CornerRadius = new(8), FontSize = 14 };

        int selectedIndex = 0;

        ImmutableArray<string> unitypackageFilePaths = UnitypackageService.GetUnitypackagePaths(ItemUtils.GetItemPath(runtimeSettings.DataRootDirectory, item.ItemPath));
        for (int i = 0; i < unitypackageFilePaths.Length; i++)
        {
            string filePath = unitypackageFilePaths[i];

            if (filePath == selectedFilePath) selectedIndex = i;

            ComboBoxItem unitypackageFileItem = new()
            {
                Content = Path.GetFileName(filePath),
                Tag = filePath
            };

            unitypackageComboBox.Items.Add(unitypackageFileItem);
            ToolTip.SetTip(unitypackageFileItem, Path.GetFileName(Path.GetDirectoryName(filePath)) + " > " + Path.GetFileName(filePath));
            ToolTip.SetBetweenShowDelay(unitypackageFileItem, -1);
        }

        if (onSelectedIndexChanged != null) unitypackageComboBox.SelectionChanged += (s, e) => onSelectedIndexChanged(id, (unitypackageComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty);
        unitypackageComboBox.SelectedIndex = selectedIndex;

        return unitypackageComboBox;
    }
}
