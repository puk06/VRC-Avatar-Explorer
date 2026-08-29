namespace AvatarExplorer.Core.Attributes;

/// <summary>
/// 列挙型のフィールドに対して、対象とするファイル拡張子のフィルタ文字列を付与する属性。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ExtensionsFilterAttribute(string filter) : Attribute
{
    /// <summary>
    /// 拡張子のフィルタ文字列。
    /// </summary>
    public string Filter { get; } = filter;
}
