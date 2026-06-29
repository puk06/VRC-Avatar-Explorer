using ReactiveUI;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ImportDataViewModel : ViewModelBase
{
    public IReactiveCommand FromV1Command { get; }
    public IReactiveCommand FromKonoAssetCommand { get; }
    public IReactiveCommand CancelCommand { get; }
}
