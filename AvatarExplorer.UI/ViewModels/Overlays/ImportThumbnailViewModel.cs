using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ImportThumbnailViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    public IReactiveCommand FromV1Command { get; }
    public IReactiveCommand FromKonoAssetCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ImportThumbnailViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);
    }
}
