using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Overlay;
using AvatarExplorer.UI.Models.System;
using AvatarExplorer.UI.Services.Utilities;
using ErrorOr;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private string? _addItemOverlay_selectedItemId = null;
    private readonly AddItemOverlayWindowValues _addItemOverlay_addItemWindowValues = new();

    private void AddItemOverlay_Open(Item item)
    {
        AddItemOverlay_InitializeCategories();

        _addItemOverlay_selectedItemId = item.Id;
        AddItemOverlay_BoothLinkTextBox.Text = item.BoothId == -1 ? string.Empty : item.GetBoothLink();

        _addItemOverlay_addItemWindowValues.ItemPaths.Clear();
        _addItemOverlay_addItemWindowValues.ItemPaths.Add(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath));
        _addItemOverlay_addItemWindowValues.FromItem(item);
        AddItemOverlay_DrawFilePathsList();
        AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);

        AddItemOverlay.IsVisible = true;

        AddItemOverlay_UpdateSupportedAvatarsLabel();
    }
    private void AddItemOverlay_Open(IEnumerable<string>? paths = null)
    {
        // もし表示されてる状態でD&Dされたら、アイテムパスの追加だけしてあげる
        if (AddItemOverlay.IsVisible && paths != null)
        {
            _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(paths);
            AddItemOverlay_DrawFilePathsList();
            return;
        }

        AddItemOverlay_InitializeCategories();

        _addItemOverlay_selectedItemId = null;
        AddItemOverlay_BoothLinkTextBox.Text = string.Empty;

        _addItemOverlay_addItemWindowValues.Reset();

        if (paths != null) _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(paths);
        AddItemOverlay_DrawFilePathsList();

        AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);

        AddItemOverlay.IsVisible = true;

        AddItemOverlay_UpdateSupportedAvatarsLabel();
    }
    private async Task AddItemOverlay_Open(LaunchInfo launchInfo)
    {
        AddItemOverlay_InitializeCategories();

        _addItemOverlay_selectedItemId = null;
        AddItemOverlay_BoothLinkTextBox.Text = string.Format(BoothLink.ItemURLWithoutAuthorFormat, launchInfo.BoothId);

        _addItemOverlay_addItemWindowValues.Reset();

        _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(launchInfo.AssetPaths);
        AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);

        AddItemOverlay.IsVisible = true;

        AddItemOverlay_DrawFilePathsList();

        AddItemOverlay_UpdateSupportedAvatarsLabel();

        await AddItemOverlay_GetBoothItemData();
    }
    private void AddItemOverlay_Close()
    {
        _addItemOverlay_selectedItemId = null;
        _addItemOverlay_addItemWindowValues.Reset();
        _editSupportedAvatarsOverlay_selectedAvatars.Clear();
        AddItemOverlay.IsVisible = false;
    }

    private void AddItemOverlay_DrawFilePathsList()
    {
        AddItemOverlay_ItemPathsList.Children.Clear();
        AddItemOverlay_ItemPathsList.RowDefinitions.Clear();

        for (int i = 0; i < _addItemOverlay_addItemWindowValues.ItemPaths.Count; i++)
        {
            string itemPath = _addItemOverlay_addItemWindowValues.ItemPaths[i];
            AddItemOverlay_DrawFilePathRow(AddItemOverlay_ItemPathsList, i, itemPath);
        }
    }
    private void AddItemOverlay_DrawFilePathRow(Grid folderListPanel, int index, string folder)
    {
        Border rowBorder = new()
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 6)
        };

        Grid itemPathGrid = new()
        {
            ColumnDefinitions = new ColumnDefinitions("30,10,*,Auto,5"),
            ColumnSpacing = 6
        };
        rowBorder.Child = itemPathGrid;

        TextBlock indexLabel = new()
        {
            Text = (index + 1).ToString(),
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeight.Bold
        };
        Grid.SetColumn(indexLabel, 0);
        itemPathGrid.Children.Add(indexLabel);

        TextBlock itemPathNameLabel = new()
        {
            Text = Path.GetFileName(folder),
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Medium,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        ToolTip.SetTip(itemPathNameLabel, folder);
        Grid.SetColumn(itemPathNameLabel, 2);
        itemPathGrid.Children.Add(itemPathNameLabel);

        Button itemRemoveButton = new()
        {
            Content = Localizer.Instance[LocalizationKey.AddItem.RemoveFolder],
            FontSize = 14,
            Padding = new Thickness(10, 4),
            Background = new SolidColorBrush(Color.FromRgb(210, 0, 0)),
            Foreground = Brushes.White,
            BorderBrush = Brushes.DarkRed,
            BorderThickness = new Thickness(1),
            Tag = folder
        };
        Grid.SetColumn(itemRemoveButton, 3);
        itemRemoveButton.Click += AddItemOverlay_FilePath_RemoveButton_Click;
        itemPathGrid.Children.Add(itemRemoveButton);

        if (_addItemOverlay_selectedItemId != null && index == 0)
        {
            itemRemoveButton.IsEnabled = false; // 親フォルダは削除できないように
        }

        Grid.SetRow(rowBorder, folderListPanel.RowDefinitions.Count);
        folderListPanel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        folderListPanel.Children.Add(rowBorder);
    }
    private void AddItemOverlay_FilePath_RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string itemPath)
        {
            _addItemOverlay_addItemWindowValues.ItemPaths.RemoveAll(i => i == itemPath);
            AddItemOverlay_DrawFilePathsList();
        }
    }

    private void AddItemOverlay_InitializeCategories()
    {
        AddItemOverlay_ItemTypeComboBox.Items.Clear();
        AddItemOverlay_ItemTypeComboBox.Items.AddRange(_avatarExplorerApp.GetCategories(includeEmptyCategory: true).Select(i => Localizer.Instance[((ItemCategory)i.Item).ToString()]));

        if (AddItemOverlay_ItemTypeComboBox.Items.Count > 0) AddItemOverlay_ItemTypeComboBox.SelectedIndex = 0;
    }

    private void AddItemOverlay_UpdateSupportedAvatarsLabel()
    {
        int totalAvatarsCount = 0;
        foreach (string avatar in _addItemOverlay_addItemWindowValues.SupportedAvatarsView)
        {
            if (avatar.StartsWith(CommonAvatar.InternalPathPrefix)) totalAvatarsCount += _avatarExplorerApp.GetCommonAvatarById(CommonAvatar.GetGroupId(avatar))?.AvatarsView.Count ?? 0;
            else totalAvatarsCount++;
        }

        AddItemOverlay_EditSupportedAvatarsButton.Content = string.Format(Localizer.Instance.Get(LocalizationKey.AddItem.SelectedAvatarsCount, totalAvatarsCount.ToString()));
    }

    private void AddItemOverlay_SetValuesToUi(AddItemOverlayWindowValues addItemWindowValues)
    {
        AddItemOverlay_BoothItemTitleTextBox.Text = addItemWindowValues.Title;
        AddItemOverlay_BoothItemAuthorTextBox.Text = addItemWindowValues.Author;
        AddItemOverlay_ItemTypeComboBox.SelectedIndex = (int)addItemWindowValues.ItemType;
        AddItemOverlay_UpdateSupportedAvatarsLabel();
        AddItemOverlay_InternalAuthorIdTextBox.Text = addItemWindowValues.BoothAuthorId;
        AddItemOverlay_InternalBoothIdTextBox.Text = addItemWindowValues.BoothId == -1 ? string.Empty : addItemWindowValues.BoothId.ToString();
        AddItemOverlay_InternalImageURLTextBox.Text = addItemWindowValues.BoothThumbnailUrl;
        AddItemOverlay_InternalAuthorImageURLTextBox.Text = addItemWindowValues.BoothAuthorThumbnailUrl;
    }
    private void AddItemOverlay_SetValuesFromUi(AddItemOverlayWindowValues addItemWindowValues)
    {
        addItemWindowValues.Title = AddItemOverlay_BoothItemTitleTextBox.Text ?? string.Empty;
        addItemWindowValues.Author = AddItemOverlay_BoothItemAuthorTextBox.Text ?? string.Empty;
        addItemWindowValues.BoothAuthorId = AddItemOverlay_InternalAuthorIdTextBox.Text ?? string.Empty;
        addItemWindowValues.BoothId = ValueParser.Int(AddItemOverlay_InternalBoothIdTextBox.Text, -1);
        addItemWindowValues.BoothThumbnailUrl = AddItemOverlay_InternalImageURLTextBox.Text ?? string.Empty;
        addItemWindowValues.BoothAuthorThumbnailUrl = AddItemOverlay_InternalAuthorImageURLTextBox.Text ?? string.Empty;
    }

    private ItemCategory AddItemOverlay_GetCurrentCategory()
    {
        int selectedIndex = AddItemOverlay_ItemTypeComboBox.SelectedIndex;

        // カスタムカテゴリかどうかのチェック(式: ItemTypeの数 - 無効なItemType数 - カスタムカテゴリ)
        if (selectedIndex >= (Enum.GetValues<ItemType>().Length - CategoryUtils.InvalidItemTypes.Length - 1))
        {
            return new ItemCategory(AddItemOverlay_ItemTypeComboBox.SelectedItem?.ToString() ?? string.Empty);
        }

        return new ItemCategory((ItemType)selectedIndex);
    }
    private bool AddItemOverlay_ValidateValues()
    {
        string errorMessage = _addItemOverlay_addItemWindowValues.Validate();
        bool result = string.IsNullOrEmpty(errorMessage);
        if (!result) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[errorMessage]);

        return result;
    }

    private async Task AddItemOverlay_GetBoothItemData()
    {
        string boothUrl = AddItemOverlay_BoothLinkTextBox.Text ?? string.Empty;

        if (_avatarExplorerApp.IsApiCooldownNow)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.BoothApiCooldown]);
            return;
        }

        ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.Booth.Status.Fetching], 0);
        ErrorOr<BoothItem> fetchResult = await _avatarExplorerApp.GetBoothItem(boothUrl);
        ProgressOverlay_Hide();

        if (fetchResult.IsError)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RetrieveBoothItemFailed]);
            return;
        }
        
        _addItemOverlay_addItemWindowValues.FromBoothItem(fetchResult.Value);
        AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);
    }

    private void AddItemOverlay_AddItemPaths(IEnumerable<string> paths)
    {
        _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(paths);
        AddItemOverlay_DrawFilePathsList();
    }

    #region Event Handler
    private async void AddItemOverlay_GetBoothItemData_Click(object? sender, RoutedEventArgs e) => await AddItemOverlay_GetBoothItemData();
    private async void AddItemOverlay_AddCustomCategory_Click(object? sender, RoutedEventArgs e)
    {
        string? customCategory = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.AddCustomCategory]);
        if (string.IsNullOrEmpty(customCategory)) return;

        int index = AddItemOverlay_ItemTypeComboBox.Items.Add(customCategory);
        AddItemOverlay_ItemTypeComboBox.SelectedIndex = index;
    }
    private async void AddItemOverlay_EditSupportedAvatars_Click(object? sender, RoutedEventArgs e)
    {
        List<string>? supportedAvatars = await EditSupportedAvatarsOverlay_OpenAsyncSafe(_addItemOverlay_addItemWindowValues.SupportedAvatarsView);
        if (supportedAvatars != null)
        {
            _addItemOverlay_addItemWindowValues.UpdateSupportedAvatars(supportedAvatars);
            AddItemOverlay_UpdateSupportedAvatarsLabel();
        }
    }
    private async void AddItemOverlay_AddFolder_Click(object? sender, RoutedEventArgs e)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], true);
        if (folders == null || folders.Length == 0) return;

        AddItemOverlay_AddItemPaths(folders);
    }
    private async void AddItemOverlay_AddFile_Click(object? sender, RoutedEventArgs e)
    {
        string[]? files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], true);
        if (files == null || files.Length == 0) return;

        AddItemOverlay_AddItemPaths(files);
    }
    private async void AddItemOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        if (_addItemOverlay_addItemWindowValues == null) return;

        AddItemOverlay_SetValuesFromUi(_addItemOverlay_addItemWindowValues);

        if (!AddItemOverlay_ValidateValues()) return;

        ItemCreationContext itemCreationContext = new();
        itemCreationContext.ItemPaths.AddRange(_addItemOverlay_addItemWindowValues.ItemPaths);
        itemCreationContext.Title = _addItemOverlay_addItemWindowValues.Title;
        itemCreationContext.Author = _addItemOverlay_addItemWindowValues.Author;
        itemCreationContext.AuthorId = _addItemOverlay_addItemWindowValues.BoothAuthorId;
        itemCreationContext.ThumbnailUrl = _addItemOverlay_addItemWindowValues.BoothThumbnailUrl;
        itemCreationContext.AuthorThumbnailUrl = _addItemOverlay_addItemWindowValues.BoothAuthorThumbnailUrl;
        itemCreationContext.BoothId = _addItemOverlay_addItemWindowValues.BoothId;

        ItemCategory itemCategory = AddItemOverlay_GetCurrentCategory();
        itemCreationContext.ItemType = itemCategory.Type;
        itemCreationContext.CustomCategory = itemCategory.CustomCategory;

        itemCreationContext.SupportedAvatars.AddRange(_addItemOverlay_addItemWindowValues.SupportedAvatarsView);

        if (_addItemOverlay_selectedItemId == null)
        {
            // 既にある同じBoothIdのアイテム
            // あればIdが入り、なければnull
            string? existingItem = _avatarExplorerApp.GetAllItems().FirstOrDefault(i => i.BoothId == itemCreationContext.BoothId)?.Id;

            if (existingItem != null)
            {
                YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.AddToExistingItem]);
                if (result == null) return;

                if (result == YesNoResult.Yes)
                {
                    ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
                    ErrorOr<ExtractResult> extractResult = await _avatarExplorerApp.AddItemPaths(existingItem, itemCreationContext.ItemPaths.ToArray());
                    ProgressOverlay_Hide();

                    if (extractResult.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.AddItemFileFailed]);
                    else if (extractResult.Value.ProcessingFailedPaths.Count > 0) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, extractResult.Value.ProcessingFailedPaths.Count.ToString()));
                    else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemFileAdd]);

                    AddItemOverlay_Close();
                    Main_ReloadCurrentWindow();

                    return;
                }
            }

            ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
            ErrorOr<ItemCreationResult> itemCreationResult = await _avatarExplorerApp.AddItem(itemCreationContext);
            ProgressOverlay_Hide();

            if (itemCreationResult.IsError)
            {
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemAddFailed]);
            }
            else if (itemCreationResult.Value.ExtractResult.ProcessingFailedPaths.Count > 0)
            {
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, itemCreationResult.Value.ExtractResult.ProcessingFailedPaths.Count.ToString()));
            }
            else if (itemCreationResult.Value.Item != null)
            {
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemAdd]);
            }
            else
            {
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemAddFailed]);
            }
        }
        else
        {
            ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
            bool result = await _avatarExplorerApp.EditItem(_addItemOverlay_selectedItemId, itemCreationContext);
            ProgressOverlay_Hide();

            if (result) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemEdit]);
            else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemEditFailed]);
        }

        AddItemOverlay_Close();
        Main_ReloadCurrentWindow();
    }
    private void AddItemOverlay_Close_Click(object? sender, RoutedEventArgs e) => AddItemOverlay_Close();
    #endregion
}
