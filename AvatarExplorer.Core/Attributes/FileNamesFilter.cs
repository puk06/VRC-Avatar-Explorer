namespace AvatarExplorer.Core.Attributes;

/// <summary>
/// 列挙型のフィールドに対して、対象とするファイル名のフィルタ文字列を付与する属性。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class FileNamesFilterAttribute(string filter) : Attribute
{
    /// <summary>
    /// ファイル名のフィルタ文字列。
    /// </summary>
    public string Filter { get; } = filter;
}
