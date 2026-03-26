using System.Globalization;
using System.Text;

namespace AvatarExplorer.Core.Utils;

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

    public static string? GetSafeTitle(string itemTitle, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(itemTitle)) return null;
        if (maxLength <= 0) return null;

        itemTitle = itemTitle.Normalize(NormalizationForm.FormC);

        StringBuilder builder = new();
        int runeCount = 0;

        foreach (var rune in itemTitle.EnumerateRunes())
        {
            if (runeCount >= maxLength) break;

            if (rune.IsInvalid()) builder.Append('_');
            else builder.Append(rune.ToString());
            runeCount++;
        }

        string safe = TrimUnsafeEdges(builder.ToString());
        if (safe.Length == 0) return null;

        string baseName = GetWindowsBaseName(safe);
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
