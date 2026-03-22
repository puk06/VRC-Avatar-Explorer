namespace AvatarExplorer.Core.Models.Items;

public enum SearchTokenType
{
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
