using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;

namespace AvatarExplorer.UI.Models.Items;

internal class UISelectableItem
{
    internal string Title { get; private set; } = string.Empty;
    internal (string LocalizationKey, string[] Args) Description { get; set; } = new();
    internal string ImageFileName { get; private set; } = string.Empty;
    internal ItemTagInfo Tag { get; private set; } = new(ItemTagStates.None, string.Empty); // ボタンが選択されたときに使用されるタグ
    internal IconType IconType { get; private set; } = IconType.None;

    internal int ItemCount { get; set; } = 0; // カテゴリなどの数表記用
    internal ImmutableArray<string>? Args { get; set; } = null;

    internal string CommonAvatarName { get; private set; } = string.Empty; // アイテム表記用
    internal string CreatedDate { get; private set; } = string.Empty; // アイテムTooltip表記用
    internal string UpdatedDate { get; private set; } = string.Empty; // アイテムTooltip表記用
    internal ImmutableArray<string> ItemTags { get; private set; } = []; // アイテムのタグ
    internal string ItemMemo { get; private set; } = string.Empty; // アイテムTooltip表記用
    internal ImmutableArray<string>? ItemFolderPaths { get; private set; } = null; // Unitypackageの一覧を取得するためのアイテムのパス一覧
    internal bool IsTempAvatar { get; private set; } = false; // 仮アバターかどうか

    internal UISelectableItem(ISelectableItem source, int itemCount, string[]? args = null)
    {
        ItemCount = itemCount;
        if (args != null) Args = args.ToImmutableArray();

        if (source is Item item) FromItem(item);
        else if (source is Author author) FromAuthor(author);
        else if (source is ItemCategory category) FromCategory(category);
        else if (source is ItemFolder itemFolder) FromFileItemFolder(itemFolder);
        else if (source is FileCategoryItem fileCategoryItem) FromFileCategoryItem(fileCategoryItem);
        else if (source is ItemFile itemFile) FromFileItemFile(itemFile);
        else if (source is CommonAvatar commonAvatar) FromCommonAvatar(commonAvatar);
        else if (source is BulkImportPreset bulkImportPreset) FromBulkImportPreset(bulkImportPreset);
        else if (source is TempAvatar tempAvatar) FromTempAvatar(tempAvatar);
    }

    internal UISelectableItem(ItemCountInfo itemCountInfo)
        : this(itemCountInfo.Item, itemCountInfo.Count, itemCountInfo.Args)
    {
    }

    internal UISelectableItem SetState(ItemTagStates state)
    {
        Tag = new ItemTagInfo(state, Tag.Value);
        return this;
    }

    private void FromItem(Item item)
    {
        Title = item.Title;
        Description = (LocalizationKey.Button.Description.Item.Author, [item.Author]);
        ImageFileName = item.ThumbnailFileName;
        Tag = new(ItemTagStates.RootSelectedItem, item.Id);
        IconType = IconType.Item;

        CreatedDate = DatetimeUtils.GetDateStringFromUnixTime(item.CreatedDate);
        UpdatedDate = DatetimeUtils.GetDateStringFromUnixTime(item.UpdatedDate);

        CommonAvatarName = Args?.Length > 0 ? (Args?[0] ?? string.Empty) : string.Empty;
        ItemTags = item.Tags;
        ItemMemo = item.ItemMemo;
        ItemFolderPaths = item.GetFolderPaths(AvatarExplorerApp.Instance.GetRuntimeSettings().DataRootDirectory).ToImmutableArray();
    }

    private void FromAuthor(Author author)
    {
        Title = author.Name;
        Description = (LocalizationKey.Button.Description.Item.Count, [ItemCount.ToString()]);
        ImageFileName = SystemIconKey.None; // 作者はアイコンなし
        Tag = new(ItemTagStates.RootAuthor, author.Name);
        IconType = IconType.None;
    }

    private void FromCategory(ItemCategory category)
    {
        Title = category.ToString();
        Description = (LocalizationKey.Button.Description.Item.Count, [ItemCount.ToString()]);
        ImageFileName = SystemIconKey.FolderIcon;
        Tag = new(ItemTagStates.RootSelectedCategory, category.Type.GetLocalizationKey() ?? category.CustomCategory);
        IconType = IconType.None;
    }
    
    private void FromFileItemFolder(ItemFolder itemFolder)
    {
        Title = itemFolder.FolderName;
        Description = (LocalizationKey.Button.Description.Item.Count, [ItemCount.ToString()]);
        ImageFileName = SystemIconKey.FolderIcon;
        Tag = new(ItemTagStates.ItemFolder, itemFolder.IsRoot ? ItemFolder.RootNodeName : itemFolder.FullPath);
        IconType = IconType.None;
    }

    private void FromFileCategoryItem(FileCategoryItem fileCategoryItem)
    {
        Title = fileCategoryItem.FileCategory.GetLocalizationKey() ?? string.Empty;
        Description = (LocalizationKey.Button.Description.Item.Count, [ItemCount.ToString()]);
        ImageFileName = SystemIconKey.FolderIcon;
        Tag = new(ItemTagStates.ItemFileCategory, fileCategoryItem.FileCategory.GetLocalizationKey() ?? string.Empty);
        IconType = IconType.None;
    }

    private void FromFileItemFile(ItemFile itemFile)
    {
        Title = itemFile.FileName;

        if (string.IsNullOrEmpty(itemFile.Extension)) Description = (LocalizationKey.Button.Description.File.NoExtension, []);
        else Description = (LocalizationKey.Button.Description.File.Extension, [itemFile.Extension]);

        ImageFileName = SystemIconKey.FileIcon;
        Tag = new(ItemTagStates.ItemFileCategoryOpen, itemFile.FullPath);
        IconType = IconType.None;
    }

    private void FromCommonAvatar(CommonAvatar commonAvatar)
    {
        Title = Localizer.Instance.Get(LocalizationKey.Button.Tag.CommonAvatar, commonAvatar.GroupName);
        Description = (LocalizationKey.Button.Description.CommonAvatar.Count, [commonAvatar.Avatars.Length.ToString()]);
        ImageFileName = SystemIconKey.GroupIcon;
        Tag = new(ItemTagStates.None, commonAvatar.GetInternalId());
        IconType = IconType.None;
    }

    private void FromBulkImportPreset(BulkImportPreset bulkImportPreset)
    {
        Title = bulkImportPreset.PresetName;
        Description = (LocalizationKey.Button.Description.BulkImportPreset.Count, [bulkImportPreset.Items.Length.ToString()]);
        ImageFileName = SystemIconKey.FolderIcon;
        Tag = new(ItemTagStates.None, bulkImportPreset.Id);
        IconType = IconType.None;
    }

    private void FromTempAvatar(TempAvatar tempAvatar)
    {
        Title = tempAvatar.AvatarName;
        Description = (LocalizationKey.Button.Description.TempAvatar, []);
        ImageFileName = SystemIconKey.AvatarIcon;
        Tag = new(ItemTagStates.None, tempAvatar.GetInternalId());
        IconType = IconType.None;

        IsTempAvatar = true;
    }
}
