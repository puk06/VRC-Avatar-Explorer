using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System;

/// <summary>指定した型の設定を JSON ファイルから読み込み・保存し、変更を通知する汎用の設定マネージャー。</summary>
/// <typeparam name="T">設定を表すクラス型。パラメータレスコンストラクタを持ち、参照型である必要があります。</typeparam>
public class SettingsManager<T>(string filePath) where T : class, new()
{
    /// <summary>設定が更新されたときに発生するイベント。更新後の設定が渡されます。</summary>
    public event Action<T>? SettingsChanged;

    private T _settings = new();

    /// <summary>現在の設定インスタンス。</summary>
    public T Settings => _settings;
    /// <summary>設定を保存する JSON ファイルのパス。</summary>
    public string FilePath { get; } = filePath;
    /// <summary>設定ファイルに記録されるマイグレーションバージョン。読み込み時に取得され、保存時に書き込まれます。</summary>
    public int MigrationVersion { get; set; }

    /// <summary>設定を新しいインスタンスに置き換え、<see cref="SettingsChanged"/> イベントを発火します。</summary>
    /// <param name="newSettings">適用する新しい設定。</param>
    public void Update(T newSettings)
    {
        _settings = newSettings;
        SettingsChanged?.Invoke(newSettings);
    }

    /// <summary>設定ファイルから JSON を読み込み、設定を復元します。ファイルが存在しない、または読み込みに失敗した場合は既定のインスタンスで初期化されます。</summary>
    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            Update(new T());
            return;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var root = JsonNode.Parse(json);

            if (root is JsonObject obj)
                MigrationVersion = obj["Version"]?.GetValue<int>() ?? 0;

            var loaded = JsonManager.Deserialize<T>(json);
            if (loaded != null) Update(loaded);
            else Update(new T());
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to load settings: '{FilePath}'.", ex);
            Update(new T());
        }
    }

    /// <summary>現在の設定を JSON ファイルに保存します。マイグレーションバージョンもファイルに書き込まれます。保存に失敗した場合は内部エラーとして記録されます。</summary>
    public void Save()
    {
        try
        {
            FileSystemService.PrepareFileDirectory(FilePath);
            var json = JsonManager.Serialize(_settings);
            var root = JsonNode.Parse(json);
            if (root is JsonObject obj)
            {
                obj["Version"] = MigrationVersion;
                json = obj.ToJsonString(JsonManager.JsonSerializerOptions);
            }
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to save settings: '{FilePath}'.", ex);
        }
    }
}
