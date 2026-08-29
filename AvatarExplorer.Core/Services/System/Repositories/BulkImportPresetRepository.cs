using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class BulkImportPresetRepository : RepositoryBase<BulkImportPreset>
{
    /// <summary>一括インポートプリセットデータのリポジトリを初期化します。</summary>
    public BulkImportPresetRepository() : base(SystemPath.BulkImportPresetDatabasePath) { }

    /// <summary>一括インポートプリセットデータベースを読み込み、必要に応じてマイグレーションを適用します。</summary>
    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            DatabaseMigrations.BulkImportPresetVersion,
            DatabaseMigrations.ApplyBulkImportPresetMigration);

        Db.Load();
        Db.MigrationVersion = DatabaseMigrations.BulkImportPresetVersion;
        InvokeUpdated();
    }

    /// <summary>指定した名前とアイテム一覧で新しい一括インポートプリセットを作成し、データベースに保存します。</summary>
    /// <param name="presetName">作成するプリセットの名前。</param>
    /// <param name="items">プリセットに含めるインポートアイテムの配列。</param>
    public void Create(string presetName, BulkImportItem[] items)
    {
        var group = new BulkImportPreset(presetName);
        group.UpdateItems(items);

        Add(group);
    }
}
