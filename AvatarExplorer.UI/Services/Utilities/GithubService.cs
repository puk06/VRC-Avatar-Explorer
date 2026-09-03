using System.Text.Json;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.UI.Services.Utilities;

public static class GithubService
{
    private const string GithubApiBaseUrl = "https://api.github.com/users/{0}";

    public static async Task<Bitmap?> GetProfileIconAsync()
    {
        try
        {
            var githubOwner = GetRepositoryOwner();
            var profileApiUrl = string.Format(GithubApiBaseUrl, githubOwner);

            using var request = new HttpRequestMessage(HttpMethod.Get, profileApiUrl);
            using var response = await HttpService.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(responseStream);

            if (!jsonDocument.RootElement.TryGetProperty("avatar_url", out var avatarUrlElement)) return null;

            var avatarUrl = avatarUrlElement.GetString();
            if (string.IsNullOrWhiteSpace(avatarUrl)) return null;

            using var avatarRequest = new HttpRequestMessage(HttpMethod.Get, avatarUrl);
            using var avatarResponse = await HttpService.Client.SendAsync(avatarRequest);
            if (!avatarResponse.IsSuccessStatusCode) return null;

            await using var avatarStream = await avatarResponse.Content.ReadAsStreamAsync();
            return new Bitmap(avatarStream);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to load developer profile icon from GitHub API.", ex);
            return null;
        }
    }
    private static string GetRepositoryOwner()
    {
        try
        {
            var repositoryUri = new Uri(SoftwareLink.RepositoryURL);
            var segments = repositoryUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length > 0 && !string.IsNullOrWhiteSpace(segments[0])) return segments[0];
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to parse repository owner from RepositoryURL.", ex);
        }

        return "puk06";
    }
}
