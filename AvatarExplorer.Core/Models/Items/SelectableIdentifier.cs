using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

public enum SelectableType
{
    None = 0,

    // RootだけはPrefixがあるため、翻訳キーを追加している。その他はそのままで大丈夫
    [LocalizationKey(LocalizationKey.Main.Path.SearchResult)]
    SearchItem = 1 << 0,

    [LocalizationKey(LocalizationKey.Main.Path.Root.Avatar)]
    RootAvatar = 1 << 1,

    [LocalizationKey(LocalizationKey.Main.Path.Root.Author)]
    RootAuthor = 1 << 2,

    [LocalizationKey(LocalizationKey.Main.Path.Root.Category)]
    RootCategory = 1 << 3,

    [LocalizationKey(LocalizationKey.Main.Path.Root.Item)]
    RootItem = 1 << 4,
    
    RootSelectedCategory = 1 << 5,
    RootSelectedItem = 1 << 6,
    ItemFolder = 1 << 7,
    ExtensionCategory = 1 << 8,
    ItemFile = 1 << 9
}

public record struct SelectableIdentifier(
    SelectableType Type,
    string Value
);
