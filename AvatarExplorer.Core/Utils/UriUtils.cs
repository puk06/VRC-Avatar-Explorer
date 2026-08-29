using System.Diagnostics.CodeAnalysis;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// URI の安全な解析を行うユーティリティを提供します。
/// </summary>
public static class UriUtils
{
    /// <summary>
    /// 文字列が有効な URI かどうかを試行し、成功した場合は結果を返します。空白のみや不正な形式の場合は false を返します。
    /// </summary>
    /// <param name="uriString">解析する URI 文字列。</param>
    /// <param name="result">解析に成功した場合は URI インスタンス、それ以外は null。</param>
    /// <returns>有効な URI として解析できた場合は true。</returns>
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
