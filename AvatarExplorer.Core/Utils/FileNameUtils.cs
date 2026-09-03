using System.Globalization;
using System.Text;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// ファイル名として安全に使用できる文字列を生成するユーティリティを提供します。
/// </summary>
public static class FileNameUtils
{
    private static readonly HashSet<char> InvalidCharsCommon = Path.GetInvalidFileNameChars()
        .Concat(Path.GetInvalidPathChars())
        .ToHashSet();

    private static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// 指定した文字列を、ファイル名として安全に使用できる形式に変換します。
    /// 不正な文字はアンダースコアに置換され、前後の空白や不要な文字が除去されます。Windows の予約名も回避されます。
    /// </summary>
    /// <param name="originalFileName">元となる文字列。</param>
    /// <param name="maxLength">最大文字数（ルーン単位）。0 以下の場合は null を返します。</param>
    /// <returns>安全なファイル名文字列。入力が空/null、または結果が空の場合は null。</returns>
    public static string? GetSafeFileName(string? originalFileName, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(originalFileName)) return null;
        if (maxLength <= 0) return null;

        originalFileName = originalFileName.Normalize(NormalizationForm.FormC);

        var builder = new StringBuilder();
        int runeCount = 0;

        foreach (var rune in originalFileName.EnumerateRunes())
        {
            if (runeCount >= maxLength) break;

            if (rune.IsInvalid()) builder.Append('_');
            else builder.Append(rune.ToString());
            runeCount++;
        }

        var safe = TrimUnsafeEdges(builder.ToString());
        if (safe.Length == 0) return null;

        var baseName = GetWindowsBaseName(safe);
        if (WindowsReservedNames.Contains(baseName)) safe = "_" + safe;

        return safe;
    }

    private static bool IsInvalid(this Rune rune)
    {
        if (rune.IsBmp)
        {
            char ch = (char)rune.Value;
            if (InvalidCharsCommon.Contains(ch)) return true;
        }

        return CharUnicodeInfo.GetUnicodeCategory(rune.Value) == UnicodeCategory.Control;
    }

    private static string TrimUnsafeEdges(string value)
    {
        int start = 0;
        int end = value.Length - 1;

        while (start <= end && char.IsWhiteSpace(value[start])) start++;
        while (end >= start && (char.IsWhiteSpace(value[end]) || value[end] == '.')) end--;

        return start > end ? string.Empty : value[start..(end + 1)];
    }

    private static string GetWindowsBaseName(string fileName)
    {
        int dotIndex = fileName.IndexOf('.');
        return dotIndex >= 0 ? fileName[..dotIndex] : fileName;
    }
}
