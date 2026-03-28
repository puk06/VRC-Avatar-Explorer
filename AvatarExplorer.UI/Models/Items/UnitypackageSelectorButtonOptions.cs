using System;
using Avalonia.Controls;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.UI.Models.Settings;

namespace AvatarExplorer.UI.Models.Items;

internal sealed class UnitypackageSelectorButtonOptions
{
    public required StackPanel Parent { get; init; }
    public required UISelectableItem Item { get; init; }
    public required RuntimeSettings RuntimeSettings { get; init; }
    public required UserPreferences UserPreferences { get; init; }
    public required string Id { get; init; }
    public required string SelectedFilePath { get; init; }
    public Action<string>? OnCopyClick { get; init; }
    public Action<string>? OnRemoveClick { get; init; }
    public Action<string, string>? OnSelectionChanged { get; init; }
}
