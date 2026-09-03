using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Models.Sort;
using AvatarExplorer.UI.Utils;

namespace AvatarExplorer.UI.Services.Sort;

public static class ItemSortService
{
    public static IEnumerable<Item> Sort(IEnumerable<Item> items, ItemSortOrder order, SortDirection direction, bool removeBrackets)
    {
        var ordered = order switch
        {
            ItemSortOrder.Title => items.OrderBy(i => removeBrackets ? TextBracketsUtils.RemoveBrackets(i.Title) : i.Title, StringComparer.OrdinalIgnoreCase),
            ItemSortOrder.Author => items.OrderBy(i => i.Author, StringComparer.OrdinalIgnoreCase),
            ItemSortOrder.CreatedDate => items.OrderBy(i => i.CreatedDate),
            ItemSortOrder.UpdatedDate => items.OrderBy(i => i.UpdatedDate),
            _ => items.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
        };

        return direction == SortDirection.Descending ? ordered.Reverse() : ordered;
    }

    public static List<IIdentifiable> SortAvatars(IEnumerable<IIdentifiable> avatars, ItemSortOrder order, SortDirection direction, bool removeBrackets, bool rawIdentifier = false)
    {
        var commonAvatars = new List<IIdentifiable>();
        var items = new List<Item>();
        var tempAvatars = new List<IIdentifiable>();

        foreach (var avatar in avatars)
        {
            if (avatar is Avatar a)
            {
                switch (a.Type)
                {
                    case AvatarType.CommonAvatar:
                        commonAvatars.Add(avatar);
                        break;
                    case AvatarType.Item:
                        items.Add((Item)a.Item);
                        break;
                    case AvatarType.TempAvatar:
                        tempAvatars.Add(avatar);
                        break;
                    default:
                        tempAvatars.Add(avatar);
                        break;
                }
            }
            else
            {
                tempAvatars.Add(avatar);
            }
        }

        var sortedItems = Sort(items, order, direction, removeBrackets)
            .Select(i => (IIdentifiable)new Avatar(i, rawIdentifier: rawIdentifier));

        return commonAvatars.Concat(sortedItems).Concat(tempAvatars).ToList();
    }
}
