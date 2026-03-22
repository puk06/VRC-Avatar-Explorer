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

public class SearchToken
{
    public static readonly char NegationPrefix = '~';
    public SearchTokenType Type { get; }
    public string Value { get; }
    public bool IsNegation { get; }

    public SearchToken(SearchTokenType type, string value)
    {
        Type = type;

        IsNegation = value.StartsWith(NegationPrefix);
        
        if (IsNegation) Value = value[1..];
        else Value = value;
    }

    public SearchToken(SearchTokenType type, string value, bool isNegation)
    {
        Type = type;
        Value = value;
        IsNegation = isNegation;
    }
}
