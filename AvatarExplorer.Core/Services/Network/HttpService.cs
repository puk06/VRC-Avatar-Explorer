using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Network;

/// <summary>
/// アプリ全体で共有する <see cref="HttpClient"/> を提供します。
/// </summary>
public static class HttpService
{
    /// <summary>
    /// アプリのユーザーエージェント（VRC-Avatar-Explorer/{バージョン}）を設定した共有の HTTP クライアント。
    /// </summary>
    public static readonly HttpClient Client = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", $"VRC-Avatar-Explorer/{AvatarExplorerApp.CurrentVersion}" }
        }
    };
}
