using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemAuthorAggregator
{
    internal static ImmutableArray<ItemCountInfo> Aggregate(IEnumerable<Item> items)
    {
        return items
            .GroupBy(i => new { i.Author, i.AuthorThumbnmailFileName })
            .Select(i => new ItemCountInfo(new Author { Name = i.Key.Author, AuthorThumbnailFileName = i.Key.AuthorThumbnmailFileName }, i.Count()))
            .ToImmutableArray();
    }
}
