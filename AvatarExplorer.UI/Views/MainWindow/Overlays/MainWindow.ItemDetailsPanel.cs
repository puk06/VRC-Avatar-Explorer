using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void ItemDetailsPanel_InitializeEventHandler()
    {
        AvatarExplorer.OnSelectionNodeChanged += ItemDetailsPanel_RenderItemDetails;
    }

    private void ItemDetailsPanel_RenderItemDetails()
    {
        if (SidePanel_ItemDetailsContent == null) return;
        SidePanel_ItemDetailsContent.RowDefinitions.Clear();
        SidePanel_ItemDetailsContent.Children.Clear();

        var selectedItem = AvatarExplorer.GetSelectedItem();
        if (selectedItem == null)
        {
            // アイテムが選択されていない場合
            SidePanel_ItemDetailsContent.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var noItemText = new TextBlock() { Text = Localizer.Instance[LocalizationKey.Error.Nothing], FontSize = 14, Opacity = 0.75, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 20, 0, 0) };
            Grid.SetRow(noItemText, 0);
            SidePanel_ItemDetailsContent.Children.Add(noItemText);
            return;
        }

        int rowIndex = 0;

        // サムネイル画像
        SidePanel_ItemDetailsContent.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var thumbnailImage = new Image() { Width = 200, Height = 200, Margin = new(0, 10, 0, 10) };

        if (!string.IsNullOrEmpty(selectedItem.ThumbnailFileName))
        {
            thumbnailImage.Source = ImageService.Get(selectedItem.ThumbnailFileName, IconType.Item);
        }
        else
        {
            thumbnailImage.Source = ImageService.Get(SystemIconKey.FileIcon);
        }

        var thumbnailPanel = new Panel();
        var thumbnailBorder = new Border() { CornerRadius = new(12), ClipToBounds = true, Child = thumbnailImage };
        thumbnailPanel.Children.Add(thumbnailBorder);

        Grid.SetRow(thumbnailPanel, rowIndex++);
        SidePanel_ItemDetailsContent.Children.Add(thumbnailPanel);

        // タイトル
        SidePanel_ItemDetailsContent.RowDefinitions.Add(new(GridLength.Auto));
        var titleText = new TextBlock() { Text = selectedItem.Title, FontSize = 18, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Colors.White), Margin = new(0, 5, 0, 0) };
        Grid.SetRow(titleText, rowIndex++);
        SidePanel_ItemDetailsContent.Children.Add(titleText);

        // 作者
        SidePanel_ItemDetailsContent.RowDefinitions.Add(new(GridLength.Auto));
        var authorText = new TextBlock() { Text = selectedItem.Author, FontSize = 13, Opacity = 0.7, Foreground = new SolidColorBrush(Colors.White), Margin = new(0, 2, 0, 12) };
        Grid.SetRow(authorText, rowIndex++);
        SidePanel_ItemDetailsContent.Children.Add(authorText);

        // Separator
        SidePanel_ItemDetailsContent.RowDefinitions.Add(new(GridLength.Auto));
        var separator = new Separator() { Margin = new(0, 5, 0, 5), Classes = { "separator" } };
        Grid.SetRow(separator, rowIndex++);
        SidePanel_ItemDetailsContent.Children.Add(separator);

        // 詳細情報
        var detailsPanel = new StackPanel() { Spacing = 8 };

        // カテゴリ
        var categoryText = selectedItem.Type == ItemType.Custom ? selectedItem.CustomCategory : Localizer.Instance[selectedItem.Type.GetLocalizationKey() ?? selectedItem.Type.ToString()];
        ItemDetailsPanel_AddDetailRow(detailsPanel, "Category", categoryText);

        // BoothID
        if (selectedItem.BoothId >= 0)
        {
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Booth ID", selectedItem.BoothId.ToString());
        }

        // タグ
        if (selectedItem.Tags.Length > 0)
        {
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Tags", string.Join(", ", selectedItem.Tags));
        }

        // サポートされているアバター
        if (selectedItem.SupportedAvatars.Length > 0)
        {
            var supportedAvatarNames = AvatarService.GetAllSupportedAvatarIds(selectedItem.SupportedAvatars, AvatarExplorer.GetAllCommonAvatars())
                .Select(id => AvatarExplorer.GetItemById(id)?.Title ?? "Unknown")
                .Select(name => RuntimeSettings.RemoveBrackets ? ItemUtils.RemoveBrackets(name) : name)
                .ToArray();

            string supportedAvatarsText = supportedAvatarNames.Length > 5 ? $"{string.Join("\n", supportedAvatarNames.Take(5))}... + {supportedAvatarNames.Length - 5}Avatars" : string.Join("\n", supportedAvatarNames);
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Supported", supportedAvatarsText);
        }

        // 実装されているアバター
        if (selectedItem.ImplementedAvatars.Length > 0)
        {
            var implementedAvatarNames = selectedItem.ImplementedAvatars
                .Select(id => AvatarExplorer.GetItemById(id)?.Title ?? "Unknown")
                .Select(name => RuntimeSettings.RemoveBrackets ? ItemUtils.RemoveBrackets(name) : name)
                .ToArray();

            string implementedAvatarsText = implementedAvatarNames.Length > 5 ? $"{string.Join("\n", implementedAvatarNames.Take(5))}... + {implementedAvatarNames.Length - 5}Avatars" : string.Join("\n", implementedAvatarNames);
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Implemented", implementedAvatarsText);
        }

        // メモ
        if (!string.IsNullOrEmpty(selectedItem.ItemMemo))
        {
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Memo", selectedItem.ItemMemo);
        }

        // 作成日
        if (!string.IsNullOrEmpty(selectedItem.CreatedDate))
        {
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Created", DatetimeUtils.GetDateStringFromUnixTime(selectedItem.CreatedDate));
        }

        // 更新日
        if (!string.IsNullOrEmpty(selectedItem.UpdatedDate))
        {
            ItemDetailsPanel_AddDetailRow(detailsPanel, "Updated", DatetimeUtils.GetDateStringFromUnixTime(selectedItem.UpdatedDate));
        }

        SidePanel_ItemDetailsContent.RowDefinitions.Add(new(GridLength.Auto));
        Grid.SetRow(detailsPanel, rowIndex + 1);
        SidePanel_ItemDetailsContent.Children.Add(detailsPanel);
    }

    private void ItemDetailsPanel_AddDetailRow(StackPanel panel, string label, string value)
    {
        var row = new Grid() { ColumnDefinitions = new("Auto,*"), ColumnSpacing = 8, Margin = new(0, 0, 0, 0) };

        var labelText = new TextBlock() { Text = $"{label}:", FontSize = 12, FontWeight = FontWeight.Bold, Opacity = 0.75, VerticalAlignment = VerticalAlignment.Top };
        Grid.SetColumn(labelText, 0);
        row.Children.Add(labelText);

        var valueText = new TextBlock() { Text = value, FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Top };
        Grid.SetColumn(valueText, 1);
        row.Children.Add(valueText);

        panel.Children.Add(row);
    }
}
