namespace AvatarExplorer.Core.Services.Database;

internal class DatabaseContainer<T>
{
    public int Version { get; set; }
    public List<T> Items { get; set; } = [];
}
