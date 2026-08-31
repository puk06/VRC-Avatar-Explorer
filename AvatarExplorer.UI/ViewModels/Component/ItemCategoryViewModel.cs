using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class ItemCategoryViewModel(ItemCategory category) : ViewModelBase
{
    [Reactive] public string DisplayName { get; set; } = string.Empty;
    public ItemCategory Category { get; } = category;

    public ItemCategoryViewModel Update()
    {
        var isCustom = Category.Type == ItemType.Custom;
        DisplayName = isCustom ? Category.ToString() : Localizer.Instance[Category.ToString()];
        return this;
    }
}
