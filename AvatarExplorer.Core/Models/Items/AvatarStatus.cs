namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// アイテムが特定のアバターに対して「対応」または「共通素体対応」かどうかのステータスを表すクラスです。<see cref="AvatarStatusResolver"/> の結果として使用されます。
/// </summary>
public class AvatarStatus
{
    /// <summary>このアイテムが対象アバターに直接対応しているかどうかを示します。</summary>
    public bool IsSupported { get; set; }
    /// <summary>このアイテムが共通素体を通じて対応しているかどうかを示します。</summary>
    public bool IsCommon { get; set; }

    /// <summary>共通素体対応の場合に、その共通素体グループの名前が設定されます。</summary>
    public string CommonAvatarName { get; set; } = string.Empty;

    /// <summary>直接対応または共通素体対応のいずれかであるかどうかを示します。</summary>
    public bool IsSupportedOrCommon => IsSupported || IsCommon;
    /// <summary>共通素体経由のみに対応し、直接の対応はないかどうかを示します。</summary>
    public bool IsOnlyCommon => IsCommon && !IsSupported;
}
