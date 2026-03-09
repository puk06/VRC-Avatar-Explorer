using System.Globalization;
using System.Text;

namespace AvatarExplorer.Core.Utils;

public static class FileNameUtils
{
    private static readonly char[] InvalidCharsCommon = Path.GetInvalidFileNameChars()
        .Concat(Path.GetInvalidPathChars())
        .Distinct()
        .ToArray();

    private static readonly string[] WindowsReservedNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    public static string? GetSafeTitle(string itemTitle, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(itemTitle)) return null;

        itemTitle = itemTitle.Normalize(NormalizationForm.FormC);

        StringBuilder builder = new();

        foreach (var rune in itemTitle.EnumerateRunes())
        {
            if (rune.IsInvalid()) builder.Append('_');
            else builder.Append(rune.ToString());
        }

        string safe = builder.ToString().Trim(' ', '　', '.');
        if (safe.Length > maxLength) safe = safe[..maxLength];

        string nameWithoutExt = Path.GetFileNameWithoutExtension(safe);
        if (WindowsReservedNames.Contains(nameWithoutExt.ToUpperInvariant())) safe = "_" + safe;

        return safe.Length == 0 ? null : safe;
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
}
