namespace AvatarExplorer.UI.Models.ContextMenu;

internal class ContextMenuAction(string name, ActionKey actionKey = ActionKey.None, ContextMenuIconType contextMenuIconType = ContextMenuIconType.None, string tag = "", bool reloadRequired = false, bool addSeparator = false)
{
    public string DisplayName { get; } = name;
    public ActionKey ActionKey { get; } = actionKey;
    public string Tag { get; } = tag;
    public ContextMenuIconType ContextMenuIconType { get; } = contextMenuIconType;
    public bool ReloadRequired { get; } = reloadRequired;
    public bool AddSeparator { get; } = addSeparator;
}
