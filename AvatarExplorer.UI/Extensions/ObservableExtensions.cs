using System.Reactive.Linq;
using AvatarExplorer.UI.Models.System;

namespace AvatarExplorer.UI.Extensions;

public static class ObservableExtensions
{
    public static IObservable<T> SkipControlled<T>(this IObservable<T> source, SkipController controller)
        => source.Where(_ => controller.ShouldPass());
}
