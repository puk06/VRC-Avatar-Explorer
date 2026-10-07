using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// <see cref="string"/> に対するローマ字関連の拡張メソッドを提供します。
/// </summary>
public static class RomajiExtensions
{
    /// <summary>
    /// 指定されたひらがなまたはカタカナの文字列をローマ字に変換します。
    /// </summary>
    /// <param name="kana">変換対象のひらがなまたはカタカナの文字列。</param>
    /// <returns>変換されたローマ字の文字列。</returns>
    public static string[] ToRomaji(this string kana) => KanaUtils.ToRomaji(kana);
}
