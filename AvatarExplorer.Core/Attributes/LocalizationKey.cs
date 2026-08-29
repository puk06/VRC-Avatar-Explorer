namespace AvatarExplorer.Core.Attributes;

/// <summary>
/// 列挙型のフィールドに対して、対応するローカライゼーションキーを付与する属性。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class LocalizationKeyAttribute(string key) : Attribute
{
    /// <summary>
    /// ローカライゼーションキー文字列。
    /// </summary>
    public string Key { get; } = key;
}
