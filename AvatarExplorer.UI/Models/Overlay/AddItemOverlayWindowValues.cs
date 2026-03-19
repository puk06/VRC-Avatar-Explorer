using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.UI.Models.Overlay;

internal class AddItemOverlayWindowValues
{
    internal List<string> ItemPaths { get; set; } = new();
    internal string Title { get; set; } = string.Empty;
    internal string Author { get; set; } = string.Empty;
    internal string BoothAuthorId { get; set; } = string.Empty;
    internal string BoothThumbnailUrl { get; set; } = string.Empty;
    internal int BoothId { get; set; } = -1;
    internal ItemType ItemType { get; set; } = ItemType.Avatar;
    internal string CustomCategory { get; set; } = string.Empty;
    private List<string> SupportedAvatars { get; set; } = new();
    internal ImmutableArray<string> SupportedAvatarsView => SupportedAvatars.ToImmutableArray();

    internal void Reset()
    {
        ItemPaths.Clear();
        Title = string.Empty;
        Author = string.Empty;
        BoothAuthorId = string.Empty;
        BoothThumbnailUrl = string.Empty;
        BoothId = -1;
        ItemType = ItemType.Avatar;
        CustomCategory = string.Empty;
        SupportedAvatars.Clear();
    }

    internal void UpdateSupportedAvatars(IEnumerable<string> newList) => SupportedAvatars = newList.ToList();

    internal void FromItem(Item item)
    {
        Title = item.Title;
        Author = item.Author;
        BoothAuthorId = item.AuthorId;
        BoothThumbnailUrl = string.Empty;
        BoothId = item.BoothId;
        ItemType = item.Type;
        CustomCategory = item.CustomCategory;

        UpdateSupportedAvatars(item.SupportedAvatarsView);
    }

    internal void FromBoothItem(BoothItem boothItem)
    {
        Title = boothItem.Title;
        Author = boothItem.Shop.Name;
        BoothAuthorId = boothItem.AuthorId;
        BoothId = boothItem.BoothId;
        BoothThumbnailUrl = boothItem.ThumbnailUrl;
        ItemType = CategoryUtils.InvalidItemTypes.Contains(boothItem.EstimatedCategory) ? ItemType.Avatar : boothItem.EstimatedCategory;
        CustomCategory = string.Empty;
    }

    internal string Validate()
    {
        if (ItemPaths.Count == 0) return LocalizationKey.Error.Validation.NoFolders;
        if (string.IsNullOrEmpty(Title)) return LocalizationKey.Error.Validation.EmptyTitle;
        if (string.IsNullOrEmpty(Author)) return LocalizationKey.Error.Validation.EmptyAuthor;

        return string.Empty;
    }
}
