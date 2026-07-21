using System.Collections.ObjectModel;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class BulkImportViewModel
{
    [Reactive] public ObservableCollection<BulkImportItemViewModel> Items { get; set; } = [];

    public IReactiveCommand CopyCommand { get; }
    public IReactiveCommand RemoveCommand { get; }
    public IReactiveCommand ImportCommand { get; }
    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand SaveCommand { get; }

    public BulkImportViewModel()
    {
        CopyCommand = ReactiveCommand.Create<BulkImportItemViewModel>(i => Items.Add(i.Copy()));
        RemoveCommand = ReactiveCommand.Create<BulkImportItemViewModel>(i => Items.Remove(i));
        ImportCommand = ReactiveCommand.Create(Import);
        ResetCommand = ReactiveCommand.Create(Reset);
        SaveCommand = ReactiveCommand.Create(Save);
    }

    private void Import()
    {
        
    }

    private void Reset()
    {
        
    }

    private void Save()
    {
        
    }

    
    public void OnBulkImportDropped(string value, bool isFile = false)
    {
        // if (isFile)
        // {
        //     var currentItem = AvatarExplorerApp.Instance.GetSelectedItem();
        //     if (currentItem == null) return;

        //     var folderPaths = currentItem.GetFolderPaths(AvatarExplorerApp.Instance.GetRuntimeSettings().DataRootDirectory);
        //     var unitypackagePaths = UnitypackageService.GetUnitypackagePaths(folderPaths);

        //     var uiSelectableItem = new UISelectableItem(currentItem);

        //     if (unitypackagePaths.Contains(value))
        //     {
        //         Items.Add(new BulkImportItemViewModel(uiSelectableItem));
        //     }
        // }
    }
}
