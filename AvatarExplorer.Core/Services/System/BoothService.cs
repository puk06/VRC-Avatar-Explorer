using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Data.Mappings;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

/// <summary>
/// Booth の商品情報を取得するサービス。商品タイトルとカテゴリからアイテムタイプを推定し、
/// 3秒の API クールダウンを管理します。
/// </summary>
public static class BoothService
{
    private static DateTime _lastBoothApiGetTime;
    /// <summary>Booth API のクールダウン（前回取得から3秒以内）中かどうかを示す値。クールダウン中は <see langword="true"/> を返します。</summary>
    public static bool IsApiCooldownNow => _lastBoothApiGetTime.AddSeconds(3) > DateTime.Now;

    private static async Task WaitForApiCooldownAsync(int pollingIntervalMs = 100, CancellationToken cancellationToken = default)
    {
        if (pollingIntervalMs < 10) pollingIntervalMs = 10;
        while (IsApiCooldownNow) await Task.Delay(pollingIntervalMs, cancellationToken);
    }
    private static async Task<ErrorOr<BoothItem>> GetItemInternal(string boothId, bool includeVariations)
    {
        try
        {
            if (boothId.Any(c => !char.IsNumber(c))) return Error.Failure(description: "The BoothId contains characters other than numbers.");

            var url = includeVariations
                ? string.Format(BoothLink.ItemWithVariationJsonURLFormat, boothId)
                : string.Format(BoothLink.ItemJsonURLFormat, boothId);
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
    private static ItemType SuggestItemType(string title, string type)
    {
        if (!BoothMapping.CategoryMappings.TryGetValue(type, out ItemType categorySuggestedType))
            categorySuggestedType = ItemType.None;

        var titleSuggestedTypes = BoothMapping.TitleMappings
            .Where(mapping => mapping.Key.Any(title.Contains))
            .Select(mapping => mapping.Value);

        return titleSuggestedTypes.Any() ? titleSuggestedTypes.First() : categorySuggestedType;
    }

    /// <summary>
    /// Booth の商品情報を取得し、アイテムタイプを推定した <see cref="BoothItem"/> を返します。
    /// <paramref name="waitCooldown"/> が true の場合は API クールダウンを自動待機し、false の場合はクールダウン中はエラーを返します。
    /// </summary>
    /// <param name="boothUrl">Booth 商品ページの URL または商品 ID。</param>
    /// <param name="waitCooldown">API クールダウンを待機するかどうか。</param>
    /// <param name="includeVariations">バリエーション情報を含めるかどうか。</param>
    /// <returns>成功した場合は推定カテゴリ付きの <see cref="BoothItem"/>、失敗した場合はエラー情報。</returns>
    public static async Task<ErrorOr<BoothItem>> Fetch(string boothUrl, bool waitCooldown = true, bool includeVariations = false)
    {
        if (string.IsNullOrEmpty(boothUrl)) return Error.Failure(description: "Invalid Url.");

        if (!waitCooldown && IsApiCooldownNow) return Error.Failure(description: "Booth API Cooldown Error.");
        else await WaitForApiCooldownAsync();

        var boothId = boothUrl.Split('/')[^1];

        _lastBoothApiGetTime = DateTime.Now;

        var result = await GetItemInternal(boothId, includeVariations: includeVariations);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch booth item.", tag: result.Errors.ToErrorString());
            return Error.Failure(description: "Failed to fetch booth item.");
        }

        return result.Value;
    }
}
