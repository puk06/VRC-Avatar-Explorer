using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvatarExplorer.Core.Models.System;
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
        var itemButton = ItemButtonFactory.CreateBaseButton(options.Item);

        var contentStackPanel = new StackPanel() { Spacing = 5 };

        var contentGrid = new Grid() { ColumnSpacing = 10, ColumnDefinitions = new("Auto,*") };

        // アイコン (アイコンにCornerRadiusを適用するため、ChildにImageが指定されたBorderが返ってくる)
        var itemIconBorder = ItemButtonFactory.CreateItemIconBorder(options.Item, options.UserPreferences, false);
        if (itemIconBorder != null)
        {
            contentGrid.Children.Add(itemIconBorder);
            Grid.SetColumn(itemIconBorder, 0);
        }

        // タイトルリスト
        var textGrid = CreateTextAndIconGrid(options.Item, options.RuntimeSettings, options.Id, options.OnCopyClick, options.OnRemoveClick);
        contentGrid.Children.Add(textGrid);
        Grid.SetColumn(textGrid, 1);

        contentStackPanel.Children.Add(contentGrid);

        var unitypackagePanel = new Panel();
        unitypackagePanel.Children.Add(CreateUnitypackageList(options.Item, options.Id, options.SelectedFilePath, options.OnSelectionChanged));

        contentStackPanel.Children.Add(unitypackagePanel);

        itemButton.Content = contentStackPanel;
        ItemButtonFactory.SetupButtonInteractions(itemButton, options.Item, options.RuntimeSettings, null, null);

        options.Parent.Children.Add(itemButton);
        return itemButton;
    }

    internal static Grid CreateTextAndIconGrid(UISelectableItem item, RuntimeSettings runtimeSettings, string id, Action<string>? onCopyClick = null, Action<string>? onRemoveClick = null)
    {
        var textGrid = new Grid() { RowDefinitions = new("Auto,Auto,5,*") };

        var itemTitle = ItemButtonFactory.GetFormattedTitle(item, runtimeSettings);

        var titleTextBlock = new TextBlock() { Text = itemTitle, FontSize = 16, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(titleTextBlock, 0);
        textGrid.Children.Add(titleTextBlock);

        var descriptionTextBlock = new TextBlock() { Text = Localizer.Instance.Get(item.Description.LocalizationKey, item.Description.Args), FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetRow(descriptionTextBlock, 1);
        textGrid.Children.Add(descriptionTextBlock);

        var iconPanel = new Panel();
        var iconStackPanel = new StackPanel() { Orientation = Orientation.Horizontal, Spacing = 5 };

        var copyButton = new Button()
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

        var removeButton = new Button()
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

    internal static ComboBox CreateUnitypackageList(UISelectableItem item, string id, string selectedFilePath, Action<string, string>? onSelectedIndexChanged = null)
    {
        var unitypackageComboBox = new ComboBox() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, CornerRadius = new(8), FontSize = 14 };

        int selectedIndex = 0;

        if (item.ItemFolderPaths != null)
        {
            var unitypackageFilePaths = UnitypackageService.GetUnitypackagePaths(item.ItemFolderPaths);
            for (int i = 0; i < unitypackageFilePaths.Length; i++)
            {
                var filePath = unitypackageFilePaths[i];

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
        }

        if (onSelectedIndexChanged != null) unitypackageComboBox.SelectionChanged += (s, e) => onSelectedIndexChanged(id, (unitypackageComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty);
        unitypackageComboBox.SelectedIndex = selectedIndex;

        return unitypackageComboBox;
    }
}
