namespace AvatarExplorer.Core.Models.Items.Internal;

internal class AvatarStatus
{
    internal bool IsSupported { get; set; }
    internal bool IsCommon { get; set; }

    internal string CommonAvatarName { get; set; } = string.Empty;
    
    internal bool IsSupportedOrCommon => IsSupported || IsCommon;
    internal bool IsOnlyCommon => IsCommon && !IsSupported;
}
