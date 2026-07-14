using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Interfaces;

public interface ISearchIndex
{
    string FreeWord { get; }
    bool IsMatch(SearchToken token);
}
