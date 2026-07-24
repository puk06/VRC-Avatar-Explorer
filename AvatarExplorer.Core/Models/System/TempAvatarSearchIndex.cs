using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Models.System;

public record TempAvatarSearchIndex : ISearchIndex
{
    public required string AvatarName { get; init; }
    public required string FreeWord { get; init; }

    public bool IsMatch(SearchQueryToken token, Func<string, string>? locKeyProvider = null)
    {
        var targets = GetTargets(token.Field);
        if (targets.Length == 0) return false;

        var comparison = StringComparison.CurrentCultureIgnoreCase;
        return token.IsNegation
            ? targets.All(t => !t.Contains(token.Value, comparison))
            : targets.Any(t => t.Contains(token.Value, comparison));
    }

    private string[] GetTargets(string? field)
    {
        return field?.ToLowerInvariant() switch
        {
            "supportedavatar" => [AvatarName],
            "avatarname" => [AvatarName],
            null => [FreeWord],
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
