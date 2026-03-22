namespace AvatarExplorer.Core.Models.Items;

public class SearchFilter
{
    public bool IsOrCondition { get; set; } = false;
    public bool IsCategoryOrCondition { get; set; } = false;
    public bool TreatEmptySupportedAvatarAsNone { get; set; } = false;
    public List<SearchToken> SearchTokens { get; } = new();
}
