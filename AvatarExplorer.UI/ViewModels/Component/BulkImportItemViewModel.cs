using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Utils;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class UnitypackageViewModel : ViewModelBase
{
    [Reactive] public string Name { get; set; } = string.Empty;
    [Reactive] public string ToolTipText { get; set; } = string.Empty;

    public string ParentDirectory { get; set; } = string.Empty;

    public UnitypackageViewModel(string path)
    {
        Name = Path.GetFileName(path) ?? path;
        ParentDirectory = Directory.GetParent(path)?.Name ?? string.Empty;

        ToolTipText = ParentDirectory + " > " + Name;
    }
}

public class BulkImportItemViewModel : ViewModelBase
{
    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string Title { get; private set; } = string.Empty;
    [Reactive] public string Description { get; private set; } = string.Empty;

    [Reactive] public double Width { get; set; } = 0;
    [Reactive] public double Height { get; set; } = 0;

    public string ImageFileName { get; set; } = string.Empty;
    public string TitleRaw { get; set; } = string.Empty;
    public bool TitleLocalizable { get; } = false;

    public LoclizableField DescriptionRaw = new();

    [Reactive] public IEnumerable<UnitypackageViewModel> UnitypackageViewModels { get; private set; } = [];
    [Reactive] public int SelectedUnitypackage { get; set; } = 0;
    public string SelectedUnitypackagePath => (SelectedUnitypackage >= 0 || SelectedUnitypackage < UnitypackageFullPaths.Length) ? UnitypackageFullPaths[SelectedUnitypackage] : string.Empty;
    
    public string[] UnitypackageFullPaths { get; set; } = [];

    public string ItemId { get; set; } = string.Empty;

    public BulkImportItemViewModel Update(int iconSize = 80, bool removeBrackets = false)
    {
        Thumbnail = ImageService.Get(ImageFileName);
        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;

        Description = DescriptionRaw.Args == null ? Localizer.Instance[DescriptionRaw.Key] : Localizer.Instance.Get(DescriptionRaw.Key, DescriptionRaw.Args);

        Width = Height = (Thumbnail != null) ? iconSize : 0;

        if (removeBrackets)
        {
            Title = TextBracketsUtils.RemoveBrackets(TitleRaw);
        }

        var previousSelectedPackage = SelectedUnitypackage;
        UnitypackageViewModels = UnitypackageFullPaths.Select(path => new UnitypackageViewModel(path));

        if (!UnitypackageViewModels.Any())
            SelectedUnitypackage = -1;
        else if (previousSelectedPackage < 0 || previousSelectedPackage >= UnitypackageViewModels.Count())
            SelectedUnitypackage = 0;
        else
            SelectedUnitypackage = previousSelectedPackage;

        return this;
    }

    public BulkImportItemViewModel Copy()
    {
        return new()
        {
            ImageFileName = ImageFileName,
            TitleRaw = TitleRaw,
            DescriptionRaw = DescriptionRaw,
            UnitypackageFullPaths = UnitypackageFullPaths,
            SelectedUnitypackage = SelectedUnitypackage,
            ItemId = ItemId
        };
    }
}
