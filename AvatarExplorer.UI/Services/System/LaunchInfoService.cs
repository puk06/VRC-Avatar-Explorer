using System;
using System.Web;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Models.System;

namespace AvatarExplorer.UI.Services.System;

public static class LaunchInfoService
{
    public static LaunchInfo? GetLaunchInfo(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            
            return new LaunchInfo
            {
                AssetPaths = query.GetValues("dir") ?? [],
                BoothId = query.Get("id") ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to parse url: '{url}.'", ex);
            return null;
        }
    }
}
