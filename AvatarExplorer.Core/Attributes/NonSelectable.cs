namespace AvatarExplorer.Core.Attributes;

/// <summary>
/// 列挙型のフィールドがナビゲーションの選択対象外（内部用途等）であることを示す属性。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class NonSelectableAttribute() : Attribute;
