using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Network;

public static class HttpService
{
    public static readonly HttpClient Client = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", $"VRC-Avatar-Explorer/{AvatarExplorerApp.CurrentVersion}" }
        }
    };
}
