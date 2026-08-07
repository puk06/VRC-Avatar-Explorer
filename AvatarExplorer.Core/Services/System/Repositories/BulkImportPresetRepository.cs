using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class BulkImportPresetRepository : RepositoryBase<BulkImportPreset>
{
    public BulkImportPresetRepository() : base(SystemPath.BulkImportPresetDatabasePath) { }

    public override void Load()
    {
        DatabaseMigrationService.Migrate(
            Db.DatabaseFilePath,
            DatabaseMigrations.BulkImportPresetVersion,
            DatabaseMigrations.ApplyBulkImportPresetMigration);

        Db.Load();
        InvokeUpdated();
    }

    public void Create(string presetName, BulkImportItem[] items)
    {
        var group = new BulkImportPreset(presetName);
        group.UpdateItems(items);

        Add(group);
    }
}
