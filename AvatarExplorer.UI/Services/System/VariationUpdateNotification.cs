using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI.Services.System;

internal static class VariationUpdateNotification
{
    internal static void Show(IReadOnlyList<VariationUpdateInfo> updates)
    {
        var contentLines = updates.Select(u =>
        {
            var variationName = string.IsNullOrEmpty(u.VariationName) ? Localizer.Instance[Loc.VariationUpdate.DefaultVariation] : u.VariationName;

            var parts = new List<string>();

            if (u.Diff.Added.Count > 0)
                parts.Add($"    {Localizer.Instance.Get(Loc.VariationUpdate.Added, u.Diff.Added.Count.ToString())}");
            if (u.Diff.Removed.Count > 0)
                parts.Add($"    {Localizer.Instance.Get(Loc.VariationUpdate.Removed, u.Diff.Removed.Count.ToString())}");
            if (u.Diff.Changed.Count > 0)
                parts.Add($"    {Localizer.Instance.Get(Loc.VariationUpdate.Changed, u.Diff.Changed.Count.ToString())}");

            return $"- {variationName}\n{string.Join("\n", parts)}";
        });

        NotificationManager.Show(
            Localizer.Instance[Loc.VariationUpdate.UpdateAvailable],
            string.Join("\n", contentLines),
            NotificationType.Information
        );
    }
}
