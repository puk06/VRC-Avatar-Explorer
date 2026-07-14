namespace AvatarExplorer.Core.Models.Items.Internal;

public class AvatarStatus
{
    public bool IsSupported { get; set; }
    public bool IsCommon { get; set; }

    public string CommonAvatarName { get; set; } = string.Empty;
    
    public bool IsSupportedOrCommon => IsSupported || IsCommon;
    public bool IsOnlyCommon => IsCommon && !IsSupported;
}
