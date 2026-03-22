namespace AvatarExplorer.Core.Models.Items;

public class SearchFilter
{
    public bool IsOrCondition { get; set; } = false;
    public List<SearchToken> SearchTokens { get; } = new();
}
