using System.Collections.Generic;

namespace AvatarExplorer.UI.Models.ContextMenu;

public class ContextMenuAction(string name, ActionKey actionKey = ActionKey.None, ContextMenuIconType contextMenuIconType = ContextMenuIconType.None, bool addSeparator = false, bool isEnabled = true)
{
    public string DisplayName { get; } = name;
    public ActionKey ActionKey { get; } = actionKey;
    public ContextMenuIconType ContextMenuIconType { get; } = contextMenuIconType;
    public bool AddSeparator { get; } = addSeparator;
    public bool IsEnabled { get; set; } = isEnabled;
    public bool UseLocalization { get; set; } = true;
    public List<ContextMenuAction> SubMenuItems { get; } = [];
}
