using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class ItemCategoryViewModel(ItemCategory category) : ViewModelBase
{
    [Reactive] public partial string DisplayName { get; set; } = string.Empty;
    public ItemCategory Category { get; } = category;

    public ItemCategoryViewModel Update()
    {
        var isCustom = Category.Type == ItemType.Custom;
        DisplayName = isCustom ? Category.ToString() : Localizer.Instance[Category.ToString()];
        return this;
    }
}
