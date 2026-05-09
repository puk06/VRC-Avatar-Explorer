using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.UI.Models.Overlay;

internal class AddItemOverlayWindowValues
{
    internal List<string> ItemPaths { get; set; } = new();
    internal string Title { get; set; } = string.Empty;
    internal string Author { get; set; } = string.Empty;
    internal string BoothAuthorId { get; set; } = string.Empty;
    internal string BoothThumbnailUrl { get; set; } = string.Empty;
    internal int BoothId { get; set; } = -1;
    internal ItemCategory Category { get; set; } = new ItemCategory(ItemType.Avatar);
    private List<string> SupportedAvatars { get; set; } = new();
    private List<string> Tags { get; set; } = new();
    internal string ItemMemo { get; set; } = string.Empty;
    internal ImmutableArray<string> SupportedAvatarsView => SupportedAvatars.ToImmutableArray();
    internal ImmutableArray<string> TagsView => Tags.ToImmutableArray();

    internal void Reset()
    {
        ItemPaths.Clear();
        Title = string.Empty;
        Author = string.Empty;
        BoothAuthorId = string.Empty;
        BoothThumbnailUrl = string.Empty;
        BoothId = -1;
        Category = new ItemCategory(ItemType.Avatar);
        SupportedAvatars.Clear();
        Tags.Clear();
        ItemMemo = string.Empty;
    }

    internal void UpdateSupportedAvatars(IEnumerable<string> newList) => SupportedAvatars = newList.ToList();
    internal void UpdateTags(IEnumerable<string> newList) => Tags = newList.ToList();

    internal void FromItem(Item item)
    {
        Title = item.Title;
        Author = item.Author;
        BoothAuthorId = item.AuthorId;
        BoothThumbnailUrl = string.Empty;
        BoothId = item.BoothId;
        Category = new ItemCategory(item.Type, item.CustomCategory);
        Tags = item.TagsView.ToList();
        ItemMemo = item.ItemMemo;

        UpdateSupportedAvatars(item.SupportedAvatarsView);
    }

    internal void FromBoothItem(BoothItem boothItem)
    {
        Title = boothItem.Title;
        Author = boothItem.Shop.Name;
        BoothAuthorId = boothItem.Shop.Id;
        BoothId = boothItem.BoothId;
        BoothThumbnailUrl = boothItem.ThumbnailUrl;
        Category = new ItemCategory(boothItem.EstimatedCategory.IsSelectable() ? boothItem.EstimatedCategory : ItemType.Avatar);
    }

    internal string Validate()
    {
        if (ItemPaths.Count == 0) return LocalizationKey.Error.Validation.NoFolders;
        if (string.IsNullOrEmpty(Title)) return LocalizationKey.Error.Validation.EmptyTitle;
        if (string.IsNullOrEmpty(Author)) return LocalizationKey.Error.Validation.EmptyAuthor;
        if (Category.Type != ItemType.Clothing && SupportedAvatars.Any(i => i.StartsWith(CommonAvatar.InternalPathPrefix))) return LocalizationKey.Error.Validation.NotClothingWithCommonAvatar;

        return string.Empty;
    }
}
