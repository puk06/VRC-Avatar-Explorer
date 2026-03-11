using System.Collections.Generic;
using System.Linq;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Models.Items;
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
    internal ItemTagInfo Tag { get; private set; } = new(); // ボタンが選択されたときに使用されるタグ
    internal IconType IconType { get; private set; } = IconType.None;

    internal int ItemCount { get; set; } = 0; // カテゴリなどの数表記用
    internal string[]? Args { get; set; } = null;

    internal string CommonAvatarName { get; private set; } = string.Empty; // アイテム表記用
    internal string CreatedDate { get; private set; } = string.Empty; // アイテムTooltip表記用
    internal string UpdatedDate { get; private set; } = string.Empty; // アイテムTooltip表記用
    private List<string> ItemTags { get; set; } = new(); // アイテムのタグ
    internal IReadOnlyList<string> ItemTagsView => ItemTags;
    internal string ItemMemo { get; private set; } = string.Empty; // アイテムTooltip表記用
    internal string ItemPath { get; private set; } = string.Empty; // Unitypackageの一覧を取得するためのアイテムのパス

    internal UISelectableItem(ISelectableItem source, int itemCount, string[]? args = null)
    {
        ItemCount = itemCount;
        if (args != null) Args = args.ToArray();

        if (source is Item item) FromItem(item);
        else if (source is Author author) FromAuthor(author);
        else if (source is ItemCategory category) FromCategory(category);
        else if (source is FileCategoryItem fileCategoryItem) FromFileCategoryItem(fileCategoryItem);
        else if (source is ItemFile itemFile) FromFileItemFile(itemFile);
        else if (source is CommonAvatar commonAvatar) FromCommonAvatar(commonAvatar);
        else if (source is BulkImportPreset bulkImportPreset) FromBulkImportPreset(bulkImportPreset);
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
        ImageFileName = item.ThumbnmailFileName;
        Tag = new(ItemTagStates.RootSelectedItem, item.Id);
        IconType = IconType.Item;

        CreatedDate = DatetimeUtils.GetDateStringFromUnixTime(item.CreatedDate);
        UpdatedDate = DatetimeUtils.GetDateStringFromUnixTime(item.UpdatedDate);

        CommonAvatarName = Args?.Length > 0 ? Args[0] : string.Empty;
        ItemTags = item.TagsView.ToList();
        ItemMemo = item.ItemMemo;
        ItemPath = item.ItemPath;
    }

    private void FromAuthor(Author author)
    {
        Title = author.Name;
        Description = (LocalizationKey.Button.Description.Item.Count, [ItemCount.ToString()]);
        ImageFileName = author.AuthorThumbnailFileName;

        Tag = new(ItemTagStates.RootAuthor, author.Name);
        IconType = IconType.Author;
    }

    private void FromCategory(ItemCategory category)
    {
        Title = category.ToString();
        Description = (LocalizationKey.Button.Description.Item.Count, [ItemCount.ToString()]);
        ImageFileName = SystemIconKey.FolderIcon;
        Tag = new(ItemTagStates.RootSelectedCategory, category.Type.GetLocalizationKey() ?? category.CustomCategory);
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
        Description = (LocalizationKey.Button.Description.File.Extension, [itemFile.Extension]);
        ImageFileName = SystemIconKey.FileIcon;
        Tag = new(ItemTagStates.ItemFileCategoryOpen, itemFile.FullPath);
        IconType = IconType.None;
    }

    private void FromCommonAvatar(CommonAvatar commonAvatar)
    {
        Title = Localizer.Instance.Get(LocalizationKey.Button.Tag.CommonAvatar, commonAvatar.GroupName);
        Description = (LocalizationKey.Button.Description.CommonAvatar.Count, [commonAvatar.AvatarsView.Count.ToString()]);
        ImageFileName = SystemIconKey.GroupIcon;
        Tag = new(ItemTagStates.None, commonAvatar.GetInternalId());
        IconType = IconType.None;
    }

    private void FromBulkImportPreset(BulkImportPreset bulkImportPreset)
    {
        Title = bulkImportPreset.PresetName;
        Description = (LocalizationKey.Button.Description.BulkImportItem.Count, [bulkImportPreset.ItemsView.Count.ToString()]);
        ImageFileName = SystemIconKey.FolderIcon;
        Tag = new(ItemTagStates.None, bulkImportPreset.Id);
        IconType = IconType.None;
    }
}
