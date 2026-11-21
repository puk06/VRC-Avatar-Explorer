using System.Collections.Immutable;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class VariationHashRepository : RepositoryBase<VariationHash>
{
    /// <summary>バリエーションのハッシュデータのリポジトリを初期化します。</summary>
    public VariationHashRepository() : base(SystemPath.VariationHashDatabasePath) { }

    /// <summary>バリエーションハッシュデータベースを読み込みます。</summary>
    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            DatabaseMigrations.VariationHashVersion,
            DatabaseMigrations.ApplyVariationHashMigration);

        Db.Load();
        InvokeUpdated();
    }

    /// <summary>指定したアイテムのバリエーションを確認し、変更があればデータベースを更新して変更情報を返します。</summary>
    /// <param name="itemId">確認対象のアイテムID。</param>
    /// <returns>変更があったバリエーションの更新情報の読み取り専用リスト。変更がない場合は空のリスト。</returns>
    public async Task<IReadOnlyList<VariationUpdateInfo>> CheckVariationAndNotify(string itemId)
    {
        var updates = await CheckAndUpdate(itemId);
        if (updates.Count == 0) return [];

        Save();
        InvokeUpdated();
        return updates;
    }

    /// <summary>指定したアイテムのバリエーションハッシュを確実に存在させ（必要に応じて取得・保存し）、以降の差分チェックを可能にします。</summary>
    /// <param name="itemId">対象のアイテムID。</param>
    /// <returns>常にtrueを返します。</returns>
    public async Task<bool> EnsureVariationHash(string itemId)
    {
        await CheckAndUpdate(itemId);
        Save();
        return true;
    }

    private async Task<List<VariationUpdateInfo>> CheckAndUpdate(string itemId)
    {
        var variationHash = GetOrCreate(itemId);
        var fetchedInfos = await FetchVariationInfos(itemId);
        if (fetchedInfos == null) return [];

        var updates = new List<VariationUpdateInfo>();
        foreach (var (variationId, info) in fetchedInfos)
        {
            var oldFiles = variationHash.VariationInfos.GetValueOrDefault(variationId)?.Files ?? [];
            var diff = CalculateDiff(oldFiles, info.Files);

            if (diff.HasChanges)
            {
                updates.Add(new VariationUpdateInfo(info.Name, diff));
            }
        }

        variationHash.UpdateAllVariations(fetchedInfos);
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

    private static async Task<Dictionary<string, VariationInfo>?> FetchVariationInfos(string itemId)
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

        return result.Value.Variations.ToDictionary(
            v => v.Id.ToString(),
            v => new VariationInfo(
                v.Name ?? string.Empty,
                v.Downloadables
                    .DistinctBy(d => d.Name)
                    .Select(d => new DownloadableFile(d.Name, HashUtils.CalculateStringHash(d.ToString())))
                    .ToImmutableArray()
            )
        );
    }

    private static VariationDiff CalculateDiff(IEnumerable<DownloadableFile> oldFiles, IEnumerable<DownloadableFile> newFiles)
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
