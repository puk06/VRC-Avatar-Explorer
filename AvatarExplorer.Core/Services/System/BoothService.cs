using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Data.Mappings;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

public static class BoothService
{
    private static DateTime _lastBoothApiGetTime;
    public static bool IsApiCooldownNow => _lastBoothApiGetTime.AddSeconds(3) > DateTime.Now;

    private static async Task WaitForApiCooldownAsync(int pollingIntervalMs = 100, CancellationToken cancellationToken = default)
    {
        if (pollingIntervalMs < 10) pollingIntervalMs = 10;
        while (IsApiCooldownNow) await Task.Delay(pollingIntervalMs, cancellationToken);
    }

    private static async Task<ErrorOr<BoothItem>> GetItem(string boothId)
    {
        try
        {
            if (boothId.Any(c => !char.IsNumber(c))) return Error.Failure(description: "The BoothId contains characters other than numbers.");

            var url = string.Format(BoothLink.ItemJsonURLFormat, boothId);
            var response = await HttpService.Client.GetStringAsync(url);

            var boothItem = JsonManager.Deserialize<BoothItem>(response);
            if (boothItem == null) return Error.Failure(description: "Failed to deserialize data.");

            return boothItem with
            {
                EstimatedCategory = new(SuggestItemType(boothItem.Title, boothItem.Category.Name)),
            };
        }
        catch (Exception)
        {
            return Error.Failure(description: $"Failed to retrieve booth item information: '{boothId}'.");
        }
    }
    private static async Task<ErrorOr<BoothItem>> GetItemWithVariations(string boothId)
    {
        try
        {
            if (boothId.Any(c => !char.IsNumber(c))) return Error.Failure(description: "The BoothId contains characters other than numbers.");

            var url = string.Format(BoothLink.ItemWithVariationJsonURLFormat, boothId);
            var response = await HttpService.Client.GetStringAsync(url);

            var boothItem = JsonManager.Deserialize<BoothItem>(response);
            if (boothItem == null) return Error.Failure(description: "Failed to deserialize data.");

            return boothItem;
        }
        catch (Exception)
        {
            return Error.Failure(description: $"Failed to retrieve booth item information: '{boothId}'.");
        }
    }
    private static ItemType SuggestItemType(string title, string type)
    {
        if (!BoothMapping.CategoryMappings.TryGetValue(type, out ItemType categorySuggestedType))
            categorySuggestedType = ItemType.None;

        var titleSuggestedTypes = BoothMapping.TitleMappings
            .Where(mapping => mapping.Key.Any(title.Contains))
            .Select(mapping => mapping.Value);

        return titleSuggestedTypes.Any() ? titleSuggestedTypes.First() : categorySuggestedType;
    }

    public static async Task<ErrorOr<BoothItem>> Fetch(string boothUrl, bool waitCooldown = true)
    {
        if (string.IsNullOrEmpty(boothUrl)) return Error.Failure(description: "Invalid Url.");
    
        if (!waitCooldown && IsApiCooldownNow) return Error.Failure(description: "Booth API Cooldown Error.");
        else await WaitForApiCooldownAsync();

        var boothId = boothUrl.Split('/')[^1];

        _lastBoothApiGetTime = DateTime.Now;

        var result = await GetItem(boothId);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch booth item.", tag: result.Errors.ToErrorString());
            return Error.Failure(description: "Failed to fetch booth item.");
        }

        return result.Value;
    }
    public static async Task<ErrorOr<BoothItem>> GetItemWithVariations(string boothUrl, bool waitCooldown = true)
    {
        if (string.IsNullOrEmpty(boothUrl)) return Error.Failure(description: "Invalid Url.");
    
        if (!waitCooldown && IsApiCooldownNow) return Error.Failure(description: "Booth API Cooldown Error.");
        else await WaitForApiCooldownAsync();

        var boothId = boothUrl.Split('/')[^1];

        _lastBoothApiGetTime = DateTime.Now;

        var result = await GetItemWithVariations(boothId);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch booth item.", tag: result.Errors.ToErrorString());
            return Error.Failure(description: "Failed to fetch booth item.");
        }

        return result.Value;
    }
}
