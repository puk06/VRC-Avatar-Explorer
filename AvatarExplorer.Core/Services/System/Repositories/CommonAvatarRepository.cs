using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class CommonAvatarRepository : RepositoryBase<CommonAvatar>
{
    /// <summary>共通素体グループデータのリポジトリを初期化します。</summary>
    public CommonAvatarRepository() : base(SystemPath.CommonAvatarDatabasePath) { }

    /// <summary>共通素体グループデータベースを読み込み、必要に応じてマイグレーションを適用します。</summary>
    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            DatabaseMigrations.CommonAvatarVersion,
            DatabaseMigrations.ApplyCommonAvatarMigration);

        Db.Load();
        Db.MigrationVersion = DatabaseMigrations.CommonAvatarVersion;
        InvokeUpdated();
    }

    /// <summary>指定した名前で新しい共通素体グループを作成し、データベースに保存します。</summary>
    /// <param name="groupName">作成する共通素体グループの名前。</param>
    public void Create(string groupName)
    {
        Add(new(groupName));
    }

    /// <summary>指定した共通素体グループに含まれるアバター一覧を上書き更新します。</summary>
    /// <param name="groupId">更新対象の共通素体グループのIdentifier（またはID）。</param>
    /// <param name="avatars">グループに設定するアバターIDの列挙可能なコレクション。</param>
    public void UpdateAvatars(string groupId, IEnumerable<string> avatars)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateAvatars(avatars);
        Save();
        InvokeUpdated();
    }

    /// <summary>指定した共通素体グループの名前を変更します。</summary>
    /// <param name="groupId">対象の共通素体グループのIdentifier（またはID）。</param>
    /// <param name="newName">変更後のグループ名。</param>
    public void RenameGroup(string groupId, string newName)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateGroupName(newName);
        Save();
        InvokeUpdated();
    }
}
