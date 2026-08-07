using System.Web;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Models.System;

namespace AvatarExplorer.UI.Services.System;

public static class BLMImportItemService
{
    public static BLMImportItemInfo? GetBLMImportItemInfo(string uriString)
    {
        if (!UriUtils.TryParse(uriString, out var uri)) return null;

        var query = HttpUtility.ParseQueryString(uri.Query);

        var dlURL = query.Get("dlurl");
        if (dlURL == null) return null;

        var dlFileName = query.Get("downloadable_filename");
        if (dlFileName == null) return null;

        var itemID = query.Get("item_id");
        if (itemID == null) return null;

        // var orderID = query.Get("order_id"); 個人情報に近づくのと、不要なので無視

        var variationID = query.Get("variation_id");
        if (variationID == null) return null;
    
        return new BLMImportItemInfo()
        {
            DownloadURL = dlURL,
            DownloadableFilename = dlFileName,
            ItemID = itemID,
            VariationID = variationID,
        };
    }
}
