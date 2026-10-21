using System.Text.Json;
using System.Web;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Models.System;

namespace AvatarExplorer.UI.Services.System;

public static class VRCAEImportItemService
{
    private const string BulkImportHost = "bulk-item-import";

    public static bool IsBulkImportUri(string uriString)
    {
        return UriUtils.TryParse(uriString, out var uri) &&
                string.Equals(uri.Scheme, SchemeService.ProtocolVRCAE, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(uri.Host, BulkImportHost, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// vrcae://bulk-item-import の URI から、同一アイテムに追加するダウンロード情報を取得します。
    /// </summary>
    public static IReadOnlyList<ImportItemInfo>? GetVRCAEImportItemInfos(string uriString)
    {
        if (!IsBulkImportUri(uriString) || !UriUtils.TryParse(uriString, out var uri)) return null;

        var query = HttpUtility.ParseQueryString(uri.Query);
        var version = query.Get("v");
        var itemID = query.Get("item_id");
        var itemsJson = query.Get("items");

        if (!string.Equals(version, "1", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(itemID) ||
            string.IsNullOrWhiteSpace(itemsJson)) return null;

        List<ImportItemInfo>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ImportItemInfo>>(itemsJson);
        }
        catch (JsonException)
        {
            return null;
        }

        if (items == null || items.Count == 0) return null;

        foreach (var item in items)
        {
            item.ItemID = itemID;

            if (string.IsNullOrWhiteSpace(item.VariationID) ||
                string.IsNullOrWhiteSpace(item.DownloadableFilename) ||
                !UriUtils.TryParse(item.DownloadURL, out var downloadUri) ||
                (downloadUri.Scheme != Uri.UriSchemeHttp && downloadUri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }
        }

        return items;
    }
}
