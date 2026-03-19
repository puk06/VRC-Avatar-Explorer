using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemAuthorAggregator
{
    internal static ImmutableArray<ItemCountInfo> Aggregate(IEnumerable<Item> items)
    {
        return items
            .DistinctBy(i => i.Author)
            .OrderBy(i => i.Author)
            .Select(i => new ItemCountInfo(new Author { Name = i.Author, AuthorThumbnailFileName = i.AuthorThumbnmailFileName }, items.Count(item => item.Author == i.Author)))
            .ToImmutableArray();
    }
}
