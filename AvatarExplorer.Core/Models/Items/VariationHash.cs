using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class VariationHash(string itemId) : AbstractDatabaseItem, IIdentifiable
{
#pragma warning disable RCS1170
    [JsonInclude] public string ItemId { get; private set; } = itemId;
#pragma warning restore RCS1170

    [JsonInclude] public Dictionary<string, List<DownloadableFile>> VariationFiles { get; private set; } = [];

    public void UpdateVariationFiles(string variationId, List<DownloadableFile> files)
    {
        VariationFiles[variationId] = files;
    }

    public void UpdateAllVariations(Dictionary<string, List<DownloadableFile>> allFiles)
    {
        VariationFiles = allFiles;
    }

    [JsonIgnore] public string Identifier => "variationHash:" + Id;
}
