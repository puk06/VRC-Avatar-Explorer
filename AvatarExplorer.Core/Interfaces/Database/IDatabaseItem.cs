namespace AvatarExplorer.Core.Interfaces.Database;

/// <summary>
/// データベースに格納されるアイテムが持つ、ID を表すインターフェース。
/// </summary>
public interface IDatabaseItem
{
    /// <summary>
    /// アイテムの ID。
    /// </summary>
    string Id { get; }
}
