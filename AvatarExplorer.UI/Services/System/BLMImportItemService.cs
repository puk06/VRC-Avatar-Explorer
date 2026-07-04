using System;
using System.Web;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Models.System;

namespace AvatarExplorer.UI.Services.System;

public static class BLMImportItemService
{
    public static BLMImportItemInfo? GetBLMImportItemInfo(string uriString)
    {
        var uri = new Uri(uriString);
        Console.WriteLine(uri.AbsolutePath);

        var query = HttpUtility.ParseQueryString(uri.Query);
        foreach (var s in query.AllKeys) { Console.WriteLine($"{s}:{query[s]}"); }


        var dlURL = query.Get("dlurl");
        var dlFileName = query.Get("downloadable_filename");
        var itemID = query.Get("item_id");
        // 個人情報に近づく可能性が高いし、アセット管理には不要な情報なので無視します。
        // _ = query.Get("order_id");
        var variationID = query.Get("variation_id");

        if (dlURL is null) { return null; }
        if (dlFileName is null) { return null; }
        if (itemID is null) { return null; }
        if (variationID is null) { return null; }

        var info = new BLMImportItemInfo()
        {
            DownloadURL = dlURL,
            DownloadableFilename = dlFileName,
            ItemID = itemID,
            VariationID = variationID,
        };
        return info;
    }
}
