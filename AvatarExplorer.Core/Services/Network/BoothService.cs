using System.Text.Json;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Data.Mappings;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.Network;

internal static class BoothService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    internal static async Task<ErrorOr<BoothItem>> GetItem(string boothId)
    {
        try
        {
            if (boothId.Any(c => !char.IsNumber(c))) return Error.Failure(description: "The BoothId contains characters other than numbers.");

            string url = string.Format(BoothLink.ItemJsonURLFormat, boothId);
            string response = await HttpService.Client.GetStringAsync(url);

            BoothItem? boothItem = JsonSerializer.Deserialize<BoothItem>(response, JsonSerializerOptions);
            if (boothItem == null) return Error.Failure(description: "Failed to deserialize data.");

            return boothItem with
            {
                EstimatedCategory = SuggestItemType(boothItem.Title, boothItem.Category.Name),
                AuthorId = BoothUtils.GetAuthorIdFromUrl(boothItem.Shop.Url)
            };
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to retrieve booth item information: '{boothId}'.", ex);
            return Error.Failure(description: "Failed to retrieve booth item information.");
        }
    }
    private static ItemType SuggestItemType(string title, string type)
    {
        if (!BoothMapping.CategoryMappings.TryGetValue(type, out ItemType categorySuggestedType))
            categorySuggestedType = ItemType.None;

        IEnumerable<ItemType> titleSuggestedTypes = BoothMapping.TitleMappings
            .Where(mapping => mapping.Key.Any(title.Contains))
            .Select(mapping => mapping.Value);

        return titleSuggestedTypes.Any() ? titleSuggestedTypes.First() : categorySuggestedType;
    }
}
