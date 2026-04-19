namespace AvatarExplorer.Core.Data.Links;

public static class BoothLink
{
    public const string ItemURLFormat = "https://{0}.booth.pm/items/{1}";
    public const string ItemURLWithoutAuthorFormat = "https://booth.pm/{0}/items/{1}";
    public const string ItemJsonURLFormat = "https://booth.pm/en/items/{0}.json"; // カテゴリなどの判定を英語で行うため、enドメインのURLを使用
    public const string AuthorURLFormat = "https://{0}.booth.pm/";
}
