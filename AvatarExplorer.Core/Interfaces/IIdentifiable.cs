namespace AvatarExplorer.Core.Interfaces;

/// <summary>
/// ナビゲーション可能なオブジェクトが実装する、一意識別子を持つためのインターフェース。
/// </summary>
public interface IIdentifiable
{
    /// <summary>
    /// オブジェクトを一意に識別する識別子（例: "item:xxxxxxxx-..."）。
    /// </summary>
    string Identifier { get; }
}
