namespace AvatarExplorer.Core.Utils;

public static class HashUtils
{
    public static string CalculateStringHash(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}
