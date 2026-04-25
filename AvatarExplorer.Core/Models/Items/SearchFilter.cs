using System.Text;

namespace AvatarExplorer.Core.Models.Items;

public class SearchFilter
{
    public bool IsOrCondition { get; set; } = false;
    public bool IsCategoryOrCondition { get; set; } = false;
    public bool TreatEmptySupportedAvatarAsNone { get; set; } = false;
    public List<SearchToken> SearchTokens { get; } = new();

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < SearchTokens.Count; i++)
        {
            var token = SearchTokens[i];
            sb.Append(token.ToString());
        }

        if (IsOrCondition) sb.Append(" OR ");
        if (IsCategoryOrCondition) sb.Append(" CategoryOR ");
        if (TreatEmptySupportedAvatarAsNone) sb.Append(" TreatEmptySupportedAvatarAsNone");

        return sb.ToString();
    }
}
