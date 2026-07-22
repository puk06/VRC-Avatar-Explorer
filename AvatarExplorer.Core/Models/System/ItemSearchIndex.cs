using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.System;

public record ItemSearchIndex : ISearchIndex
{
    public required string FreeWord { get; init; }

    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string BoothId { get; init; }
    public required string[] SupportedAvatars { get; init; }
    public required string Category { get; init; }
    public required string Memo { get; init; }
    public required string[] FolderNames { get; init; }
    public required string[] FileNames { get; init; }
    public required string[] ImplementedAvatars { get; init; }
    public required string[] NotImplementedAvatars { get; init; }
    public required string[] Tags { get; init; }
    public required string[] CommonAvatars { get; init; }

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
            SearchTokenType.Title => [Title],
            SearchTokenType.Author => [Author],
            SearchTokenType.BoothId => [BoothId],
            SearchTokenType.SupportedAvatar => SupportedAvatars,
            SearchTokenType.Category => [Category],
            SearchTokenType.ItemMemo => [Memo],
            SearchTokenType.FolderName => FolderNames,
            SearchTokenType.FileName => FileNames,
            SearchTokenType.ImplementedAvatar => ImplementedAvatars,
            SearchTokenType.NotImplementedAvatar => NotImplementedAvatars,
            SearchTokenType.Tag => Tags,
            SearchTokenType.CommonAvatar => CommonAvatars,
            SearchTokenType.FreeWord => [FreeWord],
            _ => []
        };
    }

    public static ItemSearchIndex Build(Item item, string[] supportedAvatarNames, string[] implementedAvatarNames, string[] notImplementedAvatarNames, string[] commonAvatarNames, string[] fileNames)
    {
        var category = item.Category.Type == ItemType.Custom
            ? item.Category.CustomCategory
            : item.Category.Type.GetLocalizationKey() ?? string.Empty;

        var folderNames = item.ItemPaths
            .Select(p => Path.GetFileName(p) ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();

        var freeWord = string.Join("\n",
            item.Title,
            item.Author,
            item.ItemMemo,
            item.BoothId.ToString(),
            string.Join(" ", item.Tags),
            string.Join(" ", supportedAvatarNames.Concat(implementedAvatarNames))
        ).ToLowerInvariant();

        return new ItemSearchIndex
        {
            Title = item.Title,
            Author = item.Author,
            BoothId = item.BoothId.ToString(),
            SupportedAvatars = supportedAvatarNames,
            Category = category,
            Memo = item.ItemMemo,
            FolderNames = folderNames,
            FileNames = fileNames,
            ImplementedAvatars = implementedAvatarNames,
            NotImplementedAvatars = notImplementedAvatarNames,
            Tags = item.Tags.ToArray(),
            CommonAvatars = commonAvatarNames,
            FreeWord = freeWord
        };
    }
}
