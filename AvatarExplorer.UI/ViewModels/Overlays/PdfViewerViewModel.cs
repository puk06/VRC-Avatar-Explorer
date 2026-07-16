using System.Collections.ObjectModel;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class PdfViewerViewModel : ViewModelBase
{
    
    public ObservableCollection<PdfPageViewModel> Pages { get; set; } = [];
    [Reactive] public string FileName { get; set; } = string.Empty;
    [Reactive] public string Status { get; set; } = string.Empty;

    public IReactiveCommand CloseCommand { get; }

    public PdfViewerViewModel()
    {
        CloseCommand = ReactiveCommand.Create(OnClose);
    }

    private void OnClose()
    {
        //
    }
}
