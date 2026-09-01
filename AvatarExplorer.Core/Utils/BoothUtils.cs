namespace AvatarExplorer.Core.Utils;

public static class BoothUtils
{
    public static string ExtractBoothIdFromUrl(string url)
    {
        return url.Split('/')[^1];
    }
}
