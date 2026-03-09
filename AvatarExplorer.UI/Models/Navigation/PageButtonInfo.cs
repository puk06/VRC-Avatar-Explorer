using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.UI.Models.Navigation;

internal record PageButtonInfo(ItemTagStates ItemTagState, PageButtonState Item, int NextPageValue);
