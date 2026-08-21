using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Models.System;

public record ItemSearchIndex : ISearchIndex
{
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string BoothId { get; init; }
    public required string[] SupportedAvatars { get; init; }
    public required string Category { get; init; }
    public required string Memo { get; init; }
    public required string[] ImplementedAvatars { get; init; }
    public required string[] NotImplementedAvatars { get; init; }
    public required string[] Tags { get; init; }
    public required string[] CommonAvatars { get; init; }

    public required string FreeWord { get; init; }

    public bool IsMatch(SearchQueryToken token, Func<string, string>? locKeyProvider = null)
    {
        var comparisonValue = token.Field?.ToLowerInvariant() switch
        {
            "category" when locKeyProvider != null => locKeyProvider(token.Value) ?? token.Value,
            _ => token.Value
        };

        var targets = GetTargets(token.Field);
        if (targets.Length == 0) return false;

        const StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
        return token.IsNegation
            ? targets.All(t => !t.Contains(comparisonValue, comparison))
            : targets.Any(t => t.Contains(comparisonValue, comparison));
    }

    private string[] GetTargets(string? field)
    {
        return field?.ToLowerInvariant() switch
        {
            "title" => [Title],
            "author" => [Author],
            "boothid" or "booth" => [BoothId],
            "supportedavatar" => SupportedAvatars,
            "category" => [Category],
            "memo" => [Memo],
            "implementedavatar" => ImplementedAvatars,
            "notimplementedavatar" => NotImplementedAvatars,
            "tag" => Tags,
            "commonavatar" => CommonAvatars,
            null => [FreeWord],
            _ => []
        };
    }

    public static ItemSearchIndex Build(Item item, string[] supportedAvatarNames, string[] implementedAvatarNames, string[] notImplementedAvatarNames, string[] commonAvatarNames)
    {
        var category = item.Category.ToString();

        var freeWord = string.Join("\n",
            item.Title,
            item.Author,
            item.ItemMemo,
            item.BoothId.ToString(),
            string.Join("\n", item.Tags),
            string.Join("\n", supportedAvatarNames),
            string.Join("\n", implementedAvatarNames),
            string.Join("\n", commonAvatarNames)
        ).ToLowerInvariant();

        return new ItemSearchIndex
        {
            Title = item.Title,
            Author = item.Author,
            BoothId = item.BoothId.ToString(),
            SupportedAvatars = supportedAvatarNames,
            Category = category,
            Memo = item.ItemMemo,
            ImplementedAvatars = implementedAvatarNames,
            NotImplementedAvatars = notImplementedAvatarNames,
            Tags = item.Tags.ToArray(),
            CommonAvatars = commonAvatarNames,
            FreeWord = freeWord
        };
    }
}
