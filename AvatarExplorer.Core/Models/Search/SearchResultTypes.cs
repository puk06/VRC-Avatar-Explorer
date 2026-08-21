namespace AvatarExplorer.Core.Models.Search;

[Flags]
public enum SearchResultTypes
{
    None = 0,
    Items = 1,
    CommonAvatar = 2,
    TempAvatar = 4,
    All = Items | CommonAvatar | TempAvatar
}
