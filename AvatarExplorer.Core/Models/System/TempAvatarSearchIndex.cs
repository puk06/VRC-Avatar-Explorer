using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.System;

public record TempAvatarSearchIndex : ISearchIndex
{
    public required string FreeWord { get; init; }
    public required string AvatarName { get; init; }

    public bool IsMatch(SearchToken token)
    {
        var targets = GetTargets(token.Type);
        if (targets.Length == 0)
            return token.IsNegation;

        if (token.IsNegation)
            return targets.All(t => !t.Contains(token.Value, StringComparison.CurrentCultureIgnoreCase));
        else
            return targets.Any(t => t.Contains(token.Value, StringComparison.CurrentCultureIgnoreCase));
    }

    private string[] GetTargets(SearchTokenType type)
    {
        return type switch
        {
            SearchTokenType.SupportedAvatar => [AvatarName],
            SearchTokenType.FreeWord => [FreeWord],
            _ => []
        };
    }

    public static TempAvatarSearchIndex Build(TempAvatar tempAvatar)
    {
        return new TempAvatarSearchIndex
        {
            AvatarName = tempAvatar.AvatarName,
            FreeWord = tempAvatar.AvatarName.ToLowerInvariant()
        };
    }
}
