namespace AvatarExplorer.Core.Utils;

/// <summary>
/// 文字列のハッシュ値を計算するユーティリティを提供します。
/// </summary>
public static class HashUtils
{
    /// <summary>
    /// 入力文字列の SHA256 ハッシュを計算し、小文字の16進数文字列として返します。
    /// </summary>
    /// <param name="input">ハッシュ化する入力文字列。</param>
    /// <returns>SHA256 ハッシュの16進数文字列。入力が空/null の場合は空文字列。</returns>
    public static string CalculateStringHash(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}
