namespace AvatarExplorer.Core.Models.Items;

public enum SearchTokenType
{
    None,
    Title,
    Author,
    BoothId,
    SupportedAvatar,
    Category,
    ItemMemo,
    FolderName,
    FileName,
    ImplementedAvatar,
    NotImplementedAvatar,
    Tag,
    CommonAvatar,
    FreeWord
};

public record SearchToken(SearchTokenType Type, string Value);
