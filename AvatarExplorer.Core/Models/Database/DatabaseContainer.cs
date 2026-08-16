namespace AvatarExplorer.Core.Models.Database;

internal class DatabaseContainer<T>
{
    public int Version { get; set; }
    public List<T> Items { get; set; } = [];
}
