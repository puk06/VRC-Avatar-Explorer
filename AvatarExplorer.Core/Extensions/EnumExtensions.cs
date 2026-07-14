using AvatarExplorer.Core.Attributes;

namespace AvatarExplorer.Core.Extensions;

public static class EnumExtensions
{
    private static readonly char FilterSplitter = '|';
    public static string? GetLocalizationKey(this Enum value) => value.GetAttribute<LocalizationKeyAttribute>()?.Key;
    public static string[]? GetExtensionFilters(this Enum value) => value.GetAttribute<ExtensionsFilterAttribute>()?.Filter.Split(FilterSplitter) ?? null;
    public static string[]? GetFileNameFilters(this Enum value) => value.GetAttribute<FileNamesFilterAttribute>()?.Filter.Split(FilterSplitter) ?? null;
    public static bool IsSelectable(this Enum value) => value.GetAttribute<NonSelectableAttribute>() == null;

    public static T? GetAttribute<T>(this Enum value) where T : class
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
    }
}
