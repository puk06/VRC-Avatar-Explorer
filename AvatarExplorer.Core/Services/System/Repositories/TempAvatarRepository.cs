using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class TempAvatarRepository : RepositoryBase<TempAvatar>
{
    /// <summary>仮アバターデータのリポジトリを初期化します。</summary>
    public TempAvatarRepository() : base(SystemPath.TempAvatarsDatabasePath) { }

    /// <summary>仮アバターデータベースを読み込みます。</summary>
    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            0,
            (_, _) => false);

        Db.Load();
        InvokeUpdated();
    }

    /// <summary>指定した名前で新しい仮アバターを作成し、データベースに保存します。</summary>
    /// <param name="avatarName">作成する仮アバターの名前。</param>
    public void Create(string avatarName)
    {
        Add(new(avatarName));
    }

    /// <summary>指定した仮アバターの名前を変更します。</summary>
    /// <param name="identifier">対象の仮アバターのIdentifier。</param>
    /// <param name="newName">変更後のアバター名。</param>
    public void RenameAvatar(string identifier, string newName)
    {
        var avatar = Get(identifier);
        if (avatar == null) return;

        avatar.UpdateAvatarName(newName);
        Save();
        InvokeUpdated();
    }
}
