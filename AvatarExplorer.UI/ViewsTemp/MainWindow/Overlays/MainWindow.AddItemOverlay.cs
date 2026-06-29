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
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Overlay;
using AvatarExplorer.UI.Models.System;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    // private string? _addItemOverlay_selectedItemId = null;
    // private readonly AddItemOverlayWindowValues _addItemOverlay_addItemWindowValues = new();

    // private void AddItemOverlay_Open(Item item)
    // {
    //     AddItemOverlay_InitializeCategories();

    //     _addItemOverlay_selectedItemId = item.Id;
    //     AddItemOverlay_BoothLinkTextBox.Text = item.BoothId == -1 ? string.Empty : item.GetBoothLink(Localizer.Instance[LocalizationKey.BoothLanguageCode]);

    //     _addItemOverlay_addItemWindowValues.ItemPaths.Clear();
    //     _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(item.GetFolderPaths(RuntimeSettings.DataRootDirectory, includeRootFolder: false));
    //     _addItemOverlay_addItemWindowValues.FromItem(item);
    //     AddItemOverlay_DrawFilePathsList();
    //     AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);

    //     AddItemOverlay_UpdateSupportedAvatarsLabel();
    //     AddItemOverlay_UpdateTagsLabel();
        
    //     AddItemOverlay.IsVisible = true;
    // }
    // private void AddItemOverlay_Open(IEnumerable<string>? paths = null)
    // {
    //     // もし表示されてる状態でD&Dされたら、アイテムパスの追加だけしてあげる
    //     if (AddItemOverlay.IsVisible && paths != null)
    //     {
    //         _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(paths);
    //         AddItemOverlay_DrawFilePathsList();
    //         return;
    //     }

    //     AddItemOverlay_InitializeCategories();

    //     _addItemOverlay_selectedItemId = null;
    //     AddItemOverlay_BoothLinkTextBox.Text = string.Empty;

    //     _addItemOverlay_addItemWindowValues.Reset();

    //     if (paths != null) _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(paths);
    //     AddItemOverlay_DrawFilePathsList();

    //     AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);

    //     AddItemOverlay_UpdateSupportedAvatarsLabel();
    //     AddItemOverlay_UpdateTagsLabel();
        
    //     AddItemOverlay.IsVisible = true;
    // }
    // private async Task AddItemOverlay_Open(LaunchInfo launchInfo)
    // {
    //     AddItemOverlay_InitializeCategories();

    //     _addItemOverlay_selectedItemId = null;
    //     AddItemOverlay_BoothLinkTextBox.Text = string.Format(BoothLink.ItemURLWithoutAuthorFormat, Localizer.Instance[LocalizationKey.BoothLanguageCode], launchInfo.BoothId);

    //     _addItemOverlay_addItemWindowValues.Reset();

    //     _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(launchInfo.AssetPaths);
    //     AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);

    //     AddItemOverlay_DrawFilePathsList();

    //     AddItemOverlay_UpdateSupportedAvatarsLabel();
    //     AddItemOverlay_UpdateTagsLabel();
        
    //     AddItemOverlay.IsVisible = true;

    //     await AddItemOverlay_GetBoothItemData();
    // }
    // private void AddItemOverlay_Close()
    // {
    //     AddItemOverlay.IsVisible = false;
    //     _addItemOverlay_selectedItemId = null;
    //     _addItemOverlay_addItemWindowValues.Reset();
    //     _editSupportedAvatarsOverlay_selectedAvatars.Clear();
    // }

    // private void AddItemOverlay_DrawFilePathsList()
    // {
    //     AddItemOverlay_ItemPathsList.Children.Clear();
    //     AddItemOverlay_ItemPathsList.RowDefinitions.Clear();

    //     for (int i = 0; i < _addItemOverlay_addItemWindowValues.ItemPaths.Count; i++)
    //     {
    //         string itemPath = _addItemOverlay_addItemWindowValues.ItemPaths[i];
    //         AddItemOverlay_DrawFilePathRow(AddItemOverlay_ItemPathsList, i, itemPath);
    //     }
    // }
    // private void AddItemOverlay_DrawFilePathRow(Grid folderListPanel, int index, string folder)
    // {
    //     var rowBorder = new Border()
    //     {
    //         BorderBrush = Brushes.Gray,
    //         BorderThickness = new Thickness(0, 0, 0, 1),
    //         Padding = new Thickness(8, 6)
    //     };

    //     var itemPathGrid = new Grid()
    //     {
    //         ColumnDefinitions = new ColumnDefinitions("30,10,*,Auto,5"),
    //         ColumnSpacing = 6
    //     };
    //     rowBorder.Child = itemPathGrid;

    //     var indexLabel = new TextBlock()
    //     {
    //         Text = (index + 1).ToString(),
    //         FontSize = 16,
    //         VerticalAlignment = VerticalAlignment.Center,
    //         HorizontalAlignment = HorizontalAlignment.Center,
    //         FontWeight = FontWeight.Bold
    //     };
    //     Grid.SetColumn(indexLabel, 0);
    //     itemPathGrid.Children.Add(indexLabel);

    //     var itemPathNameLabel = new TextBlock()
    //     {
    //         Text = Path.GetFileName(folder),
    //         FontSize = 16,
    //         VerticalAlignment = VerticalAlignment.Center,
    //         FontWeight = FontWeight.Medium,
    //         TextTrimming = TextTrimming.CharacterEllipsis
    //     };
    //     ToolTip.SetTip(itemPathNameLabel, folder);
    //     Grid.SetColumn(itemPathNameLabel, 2);
    //     itemPathGrid.Children.Add(itemPathNameLabel);

    //     var itemRemoveButton = new Button()
    //     {
    //         Content = Localizer.Instance[LocalizationKey.AddItem.RemoveFolder],
    //         FontSize = 14,
    //         Padding = new Thickness(10, 4),
    //         BorderThickness = new Thickness(1),
    //         Tag = folder
    //     };
    //     itemRemoveButton.Classes.Add("accentbutton");
    //     Grid.SetColumn(itemRemoveButton, 3);
    //     itemRemoveButton.Click += AddItemOverlay_FilePath_RemoveButton_Click;
    //     itemPathGrid.Children.Add(itemRemoveButton);

    //     Grid.SetRow(rowBorder, folderListPanel.RowDefinitions.Count);
    //     folderListPanel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
    //     folderListPanel.Children.Add(rowBorder);
    // }
    // private void AddItemOverlay_FilePath_RemoveButton_Click(object? sender, RoutedEventArgs e)
    // {
    //     if (sender is Button button && button.Tag is string itemPath)
    //     {
    //         _addItemOverlay_addItemWindowValues.ItemPaths.RemoveAll(i => i == itemPath);
    //         AddItemOverlay_DrawFilePathsList();
    //     }
    // }

    // private void AddItemOverlay_InitializeCategories()
    // {
    //     AddItemOverlay_ItemTypeComboBox.Items.Clear();

    //     var categories = AvatarExplorer.GetCategories(includeEmptyCategory: true, includeAllCategory: false).Select(i => (ItemCategory)i.Item);
    //     foreach (var category in categories)
    //     {
    //         var categoryName = Localizer.Instance[category.ToString()];
    //         AddItemOverlay_ItemTypeComboBox.Items.Add(new ComboBoxItem()
    //         {
    //             Content = categoryName,
    //             Tag = category
    //         });
    //     }

    //     if (AddItemOverlay_ItemTypeComboBox.Items.Count > 0) AddItemOverlay_ItemTypeComboBox.SelectedIndex = 0;
    // }

    // private void AddItemOverlay_UpdateSupportedAvatarsLabel()
    // {
    //     int totalAvatarsCount = 0;
    //     foreach (var avatar in _addItemOverlay_addItemWindowValues.SupportedAvatars)
    //     {
    //         if (avatar.StartsWith(CommonAvatar.InternalPathPrefix)) totalAvatarsCount += AvatarExplorer.GetCommonAvatarById(CommonAvatar.GetGroupId(avatar))?.Avatars.Length ?? 0;
    //         else totalAvatarsCount++;
    //     }

    //     AddItemOverlay_EditSupportedAvatarsButton.Content = string.Format(Localizer.Instance.Get(LocalizationKey.AddItem.SelectedAvatarsCount, totalAvatarsCount.ToString()));
    // }
    // private void AddItemOverlay_UpdateTagsLabel()
    // {
    //     AddItemOverlay_EditTagsButton.Content = string.Format(Localizer.Instance.Get(LocalizationKey.AddItem.SelectedTagsCount, _addItemOverlay_addItemWindowValues.Tags.Length.ToString()));
    // }

    // private void AddItemOverlay_SetValuesToUi(AddItemOverlayWindowValues addItemWindowValues)
    // {
    //     AddItemOverlay_BoothItemTitleTextBox.Text = addItemWindowValues.Title;
    //     AddItemOverlay_BoothItemAuthorTextBox.Text = addItemWindowValues.Author;
    //     AddItemOverlay_ItemTypeComboBox.SelectedIndex = AddItemOverlay_GetCategoryIndex(addItemWindowValues.Category);
    //     AddItemOverlay_UpdateSupportedAvatarsLabel();
    //     AddItemOverlay_UpdateTagsLabel();
    //     AddItemOverlay_InternalAuthorIdTextBox.Text = addItemWindowValues.BoothAuthorId;
    //     AddItemOverlay_InternalBoothIdTextBox.Text = addItemWindowValues.BoothId == -1 ? string.Empty : addItemWindowValues.BoothId.ToString();
    //     AddItemOverlay_InternalImageURLTextBox.Text = addItemWindowValues.BoothThumbnailUrl;
    // }
    // private void AddItemOverlay_SetValuesFromUi(AddItemOverlayWindowValues addItemWindowValues)
    // {
    //     addItemWindowValues.Title = AddItemOverlay_BoothItemTitleTextBox.Text ?? string.Empty;
    //     addItemWindowValues.Author = AddItemOverlay_BoothItemAuthorTextBox.Text ?? string.Empty;
    //     addItemWindowValues.Category = AddItemOverlay_GetItemCategoryFromIndex(AddItemOverlay_ItemTypeComboBox.SelectedIndex);
    //     addItemWindowValues.BoothAuthorId = AddItemOverlay_InternalAuthorIdTextBox.Text ?? string.Empty;
    //     addItemWindowValues.BoothId = ValueParser.Int(AddItemOverlay_InternalBoothIdTextBox.Text, -1);
    //     addItemWindowValues.BoothThumbnailUrl = AddItemOverlay_InternalImageURLTextBox.Text ?? string.Empty;
    // }

    // private int AddItemOverlay_GetCategoryIndex(ItemCategory category)
    // {
    //     for (int i = 0; i < AddItemOverlay_ItemTypeComboBox.Items.Count; i++)
    //     {
    //         if (AddItemOverlay_ItemTypeComboBox.Items[i] is ComboBoxItem comboBoxItem && comboBoxItem.Tag is ItemCategory itemCategory && itemCategory.Equals(category))
    //         {
    //             return i;
    //         }
    //     }

    //     return 0; // 見つからなかったらとりあえず先頭のカテゴリを選択しておく
    // }
    // private ItemCategory AddItemOverlay_GetItemCategoryFromIndex(int index)
    // {
    //     if (index < 0 || index >= AddItemOverlay_ItemTypeComboBox.Items.Count) return new ItemCategory();

    //     if (AddItemOverlay_ItemTypeComboBox.Items[index] is ComboBoxItem comboBoxItem && comboBoxItem.Tag is ItemCategory itemCategory)
    //     {
    //         return itemCategory;
    //     }

    //     return new ItemCategory();
    // }

    // private bool AddItemOverlay_ValidateValues()
    // {
    //     var errorMessage = _addItemOverlay_addItemWindowValues.Validate();
    //     var result = string.IsNullOrEmpty(errorMessage);
    //     if (!result) Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[errorMessage], isError: true);

    //     return result;
    // }

    // private async Task AddItemOverlay_GetBoothItemData()
    // {
    //     var boothUrl = AddItemOverlay_BoothLinkTextBox.Text ?? string.Empty;

    //     if (AvatarExplorer.IsApiCooldownNow)
    //     {
    //         Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.BoothApiCooldown], isError: true);
    //         return;
    //     }

    //     ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.Booth.Status.Fetching], 0);
    //     var fetchResult = await AvatarExplorer.GetBoothItem(boothUrl);
    //     ProgressOverlay_Hide();

    //     if (fetchResult.IsError)
    //     {
    //         Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RetrieveBoothItemFailed], isError: true);
    //         return;
    //     }
        
    //     _addItemOverlay_addItemWindowValues.FromBoothItem(fetchResult.Value);
    //     AddItemOverlay_SetValuesToUi(_addItemOverlay_addItemWindowValues);
    // }

    // private void AddItemOverlay_AddItemPaths(IEnumerable<string> paths)
    // {
    //     _addItemOverlay_addItemWindowValues.ItemPaths.AddRange(paths);
    //     AddItemOverlay_DrawFilePathsList();
    // }

    // #region Event Handler
    // private async void AddItemOverlay_GetBoothItemData_Click(object? sender, RoutedEventArgs e) => await AddItemOverlay_GetBoothItemData();
    // private async void AddItemOverlay_AddCustomCategory_Click(object? sender, RoutedEventArgs e)
    // {
    //     var customCategory = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.AddCustomCategory]);
    //     if (string.IsNullOrEmpty(customCategory)) return;

    //     int index = AddItemOverlay_ItemTypeComboBox.Items.Add(new ComboBoxItem()
    //     {
    //         Content = customCategory,
    //         Tag = new ItemCategory(customCategory)
    //     });
    //     AddItemOverlay_ItemTypeComboBox.SelectedIndex = index;
    // }
    // private async void AddItemOverlay_EditSupportedAvatars_Click(object? sender, RoutedEventArgs e)
    // {
    //     var supportedAvatars = await EditSupportedAvatarsOverlay_OpenAsyncSafe(_addItemOverlay_addItemWindowValues.SupportedAvatars);
    //     if (supportedAvatars != null)
    //     {
    //         _addItemOverlay_addItemWindowValues.UpdateSupportedAvatars(supportedAvatars);
    //         AddItemOverlay_UpdateSupportedAvatarsLabel();
    //     }
    // }
    // private async void AddItemOverlay_EditItemMemo_Click(object? sender, RoutedEventArgs e)
    // {
    //     var memo = await EditMemoOverlay_ShowSafeAsync(_addItemOverlay_addItemWindowValues.ItemMemo);
    //     if (memo == null) return;

    //     _addItemOverlay_addItemWindowValues.ItemMemo = memo;
    // }
    // private async void AddItemOverlay_EditTags_Click(object? sender, RoutedEventArgs e)
    // {
    //     var tags = await EditTagsOverlay_ShowAsyncSafe(_addItemOverlay_addItemWindowValues.Tags);
    //     if (tags != null)
    //     {
    //         _addItemOverlay_addItemWindowValues.UpdateTags(tags);
    //         AddItemOverlay_UpdateTagsLabel();
    //     }
    // }

    // private async void AddItemOverlay_AddFolder_Click(object? sender, RoutedEventArgs e)
    // {
    //     var folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], true);
    //     if (folders == null || folders.Length == 0) return;

    //     AddItemOverlay_AddItemPaths(folders);
    // }
    // private async void AddItemOverlay_AddFile_Click(object? sender, RoutedEventArgs e)
    // {
    //     var files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], true);
    //     if (files == null || files.Length == 0) return;

    //     AddItemOverlay_AddItemPaths(files);
    // }

    // private async void AddItemOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    // {
    //     if (_addItemOverlay_addItemWindowValues == null) return;

    //     AddItemOverlay_SetValuesFromUi(_addItemOverlay_addItemWindowValues);

    //     if (!AddItemOverlay_ValidateValues()) return;

    //     var itemCreationContext = new ItemCreationContext();
    //     itemCreationContext.ItemPaths.AddRange(_addItemOverlay_addItemWindowValues.ItemPaths);
    //     itemCreationContext.Title = _addItemOverlay_addItemWindowValues.Title;
    //     itemCreationContext.Author = _addItemOverlay_addItemWindowValues.Author;
    //     itemCreationContext.AuthorId = _addItemOverlay_addItemWindowValues.BoothAuthorId;
    //     itemCreationContext.ThumbnailUrl = _addItemOverlay_addItemWindowValues.BoothThumbnailUrl;
    //     itemCreationContext.BoothId = _addItemOverlay_addItemWindowValues.BoothId;

    //     itemCreationContext.ItemType = _addItemOverlay_addItemWindowValues.Category.Type;
    //     itemCreationContext.CustomCategory = _addItemOverlay_addItemWindowValues.Category.CustomCategory;

    //     itemCreationContext.SupportedAvatars.AddRange(_addItemOverlay_addItemWindowValues.SupportedAvatars);
    //     itemCreationContext.Tags.AddRange(_addItemOverlay_addItemWindowValues.Tags);
    //     itemCreationContext.ItemMemo = _addItemOverlay_addItemWindowValues.ItemMemo;

    //     if (_addItemOverlay_selectedItemId == null)
    //     {
    //         // 既にある同じBoothIdのアイテム
    //         // あればIdが入り、なければnull
    //         var existingItem = itemCreationContext.BoothId == -1 ? null : AvatarExplorer.GetAllItems().FirstOrDefault(i => i.BoothId == itemCreationContext.BoothId)?.Id;

    //         if (existingItem != null)
    //         {
    //             var result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.AddToExistingItem]);
    //             if (result == null) return;

    //             if (result == YesNoResult.Yes)
    //             {
    //                 ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
    //                 var extractResult = await AvatarExplorer.AddItemPaths(existingItem, itemCreationContext.ItemPaths.ToArray());
    //                 ProgressOverlay_Hide();

    //                 if (extractResult.IsError) Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.AddItemFileFailed], isError: true);
    //                 else if (extractResult.Value.ProcessingFailedPaths.Count > 0) Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, extractResult.Value.ProcessingFailedPaths.Count.ToString()), isError: true);
    //                 else Main_ShowNotification(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemFileAdd], isSuccess: true);

    //                 AddItemOverlay_Close();
    //                 Main_ReloadCurrentWindow();

    //                 return;
    //             }
    //         }

    //         ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
    //         var itemCreationResult = await AvatarExplorer.AddItem(itemCreationContext);
    //         ProgressOverlay_Hide();

    //         if (itemCreationResult.IsError)
    //         {
    //             Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemAddFailed], isError: true);
    //         }
    //         else if (itemCreationResult.Value.ExtractResult.ProcessingFailedPaths.Count > 0)
    //         {
    //             Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, itemCreationResult.Value.ExtractResult.ProcessingFailedPaths.Count.ToString()), isError: true);
    //         }
    //         else if (itemCreationResult.Value.Item != null)
    //         {
    //             Main_ShowNotification(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemAdd], isSuccess: true);
    //         }
    //         else
    //         {
    //             Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemAddFailed], isError: true);
    //         }
    //     }
    //     else
    //     {
    //         ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
    //         var result = await AvatarExplorer.EditItem(_addItemOverlay_selectedItemId, itemCreationContext);
    //         ProgressOverlay_Hide();

    //         if (result) Main_ShowNotification(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemEdit], isSuccess: true);
    //         else Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemEditFailed], isError: true);
    //     }

    //     AddItemOverlay_Close();
    //     Main_ReloadCurrentWindow();
    // }
    // private void AddItemOverlay_Close_Click(object? sender, RoutedEventArgs e) => AddItemOverlay_Close();
    // #endregion
}
