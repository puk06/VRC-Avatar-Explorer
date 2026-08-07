using System.Diagnostics.CodeAnalysis;

namespace AvatarExplorer.Core.Utils;

public static class UriUtils
{
    public static bool TryParse(string uriString, [NotNullWhen(true)] out Uri? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(uriString)) return false;

        try
        {
            result = new Uri(uriString);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
