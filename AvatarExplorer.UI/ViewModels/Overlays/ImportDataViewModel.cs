using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ImportDataViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    public IReactiveCommand FromV1Command { get; }
    public IReactiveCommand FromKonoAssetCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ImportDataViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);
    }
}
