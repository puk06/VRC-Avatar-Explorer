using System.Reflection;
using AvatarExplorer.Core.Attributes;

namespace AvatarExplorer.Core.Extensions;

public static class EnumExtensions
{
    public static string? GetLocalizationKey(this Enum value) => value.GetAttribute<LocalizationKeyAttribute>()?.Key;
    internal static string[]? GetExtensionFilters(this Enum value) => value.GetAttribute<ExtensionsFilterAttribute>()?.Filter.Split('|') ?? null;
    internal static string[]? GetFileNameFilters(this Enum value) => value.GetAttribute<FileNamesFilterAttribute>()?.Filter.Split('|') ?? null;

    public static T? GetAttribute<T>(this Enum value) where T : class
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
    }
}
