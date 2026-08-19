using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class VariationHashRepository : RepositoryBase<VariationHash>
{
    public VariationHashRepository() : base(SystemPath.VariationHashDatabasePath) { }

    public override void Load()
    {
        Db.Load();
        InvokeUpdated();
    }

    public async Task<IReadOnlyList<VariationUpdateInfo>> CheckVariationAndNotify(string itemId)
    {
        var updates = await CheckAndUpdate(itemId);
        if (updates.Count == 0) return [];

        Save();
        InvokeUpdated();
        return updates;
    }
    
    public class VariationData
    {
        public string VariationId { get; init; } = string.Empty;
        public string VariationName { get; init; } = string.Empty;
        public List<DownloadableFile> Files { get; init; } = [];
    }

    public async Task<bool> EnsureVariationHash(string itemId)
    {
        await CheckAndUpdate(itemId);
        Save();
        return true;
    }

    private async Task<List<VariationUpdateInfo>> CheckAndUpdate(string itemId)
    {
        var variationHash = GetOrCreate(itemId);
        var fetchedData = await FetchVariationData(itemId);
        if (fetchedData == null) return [];

        var updates = new List<VariationUpdateInfo>();
        foreach (var variationData in fetchedData)
        {
            var oldFiles = variationHash.VariationFiles.GetValueOrDefault(variationData.VariationId) ?? [];
            var newFiles = variationData.Files;
            var diff = CalculateDiff(oldFiles, newFiles);

            if (diff.HasChanges)
            {
                updates.Add(new VariationUpdateInfo(variationData.VariationName, diff));
            }
        }

        variationHash.UpdateAllVariations(fetchedData.ToDictionary(kv => kv.VariationId, kv => kv.Files));
        return updates;
    }

    private VariationHash GetOrCreate(string itemId)
    {
        var existing = GetAll().FirstOrDefault(vh => vh.ItemId == itemId);
        if (existing != null) return existing;

        var created = new VariationHash(itemId);
        Add(created);
        return created;
    }

    private static async Task<List<VariationData>?> FetchVariationData(string itemId)
    {
        var result = await BoothService.Fetch(itemId, includeVariations: true);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError(
                $"Failed to fetch booth item for variation check: '{itemId}'.",
                tag: result.Errors.ToErrorString()
            );
            return null;
        }

        return result.Value.Variations.Select(v => new VariationData
        {
            VariationId = v.Id.ToString(),
            VariationName = v.Name ?? string.Empty,
            Files = v.Downloadables
                .DistinctBy(d => d.Name)
                .Select(d => new DownloadableFile(d.Name, HashUtils.CalculateStringHash(d.ToString()))).ToList()
        }).ToList();
    }
    private static VariationDiff CalculateDiff(List<DownloadableFile> oldFiles, List<DownloadableFile> newFiles)
    {
        var oldDict = oldFiles.ToDictionary(f => f.FileName, f => f.Hash);
        var newDict = newFiles.ToDictionary(f => f.FileName, f => f.Hash);

        var added = newFiles.Where(f => !oldDict.ContainsKey(f.FileName)).ToList();
        var removed = oldFiles.Where(f => !newDict.ContainsKey(f.FileName)).ToList();
        var changed = newFiles
            .Where(f => oldDict.TryGetValue(f.FileName, out var oldHash) && oldHash != f.Hash)
            .ToList();

        return new VariationDiff(added, removed, changed);
    }
}
