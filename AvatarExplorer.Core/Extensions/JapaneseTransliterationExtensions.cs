using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// <see cref="string"/> 型の文字列に対する日本語の音韻変換（ローマ字・ひらがな）関連の拡張メソッドを提供します。
/// </summary>
public static class JapaneseTransliterationExtensions
{
    /// <summary>
    /// 指定されたひらがなまたはカタカナの文字列をローマ字に変換します。
    /// </summary>
    /// <param name="kana">変換対象のひらがなまたはカタカナの文字列。</param>
    /// <returns>変換されたローマ字の文字列。</returns>
    public static string[] ToRomaji(this string kana) => JapanesePhoneticsHelper.ToRomaji(kana);

    /// <summary>
    /// 指定されたひらがなまたはカタカナの文字列をひらがなに変換します。
    /// </summary>
    /// <param name="kana">変換対象のひらがなまたはカタカナの文字列。</param>
    /// <returns>変換されたひらがなの文字列。</returns>
    public static string ToHiragana(this string kana) => JapanesePhoneticsHelper.ToHiragana(kana);
}
