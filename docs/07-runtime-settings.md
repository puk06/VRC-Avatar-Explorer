# 07 - 設定の管理 (RuntimeSettings)

この章では、`RuntimeSettings`と`RuntimeSettingsRepository`を使った設定の管理について学びます。

## RuntimeSettingsの基本

`RuntimeSettings`は、AvatarExplorer.Coreの基本的な設定を保持するレコードクラスです。

```csharp
var app = AvatarExplorerApp.Instance;
app.Initialize();

// 設定の取得（ショートカット）
var settings = app.RuntimeSettings;

// または、Repository経由で取得
var settings2 = app.RuntimeSettingsRepository.Settings;
```

## 設定項目一覧

```csharp
public record RuntimeSettings
{
    // データの保存先ルートディレクトリ
    public string DataRootDirectory { get; init; }

    // 自動バックアップの保存先ディレクトリ
    public string AutoBackupRootDirectory { get; init; }

    // インポート時に元のファイルを削除するか
    public bool RemoveOriginal { get; init; }

    // 元のファイルへのリンクを作成するか（コピーしない）
    public bool ShouldLinkToOriginal { get; init; }

    // 自動バックアップの間隔（日数）
    public int AutoBackupInterval { get; init; }

    // 対応アバターが空の場合に「なし」として扱うか
    public bool TreatEmptySupportedAvatarAsNone { get; init; }

    // 最大並列処理数
    public int MaxDegreeOfParallelism { get; init; }

    // Unitypackageのパスを自動変更するか
    public bool AutoChangeUnitypackagePath { get; init; }

    // アップデートを自動チェックするか
    public bool CheckForUpdate { get; init; }

    // アップデートチャンネル
    public UpdateChannel UpdateChannel { get; init; }
}
```

### デフォルト値

| 設定 | デフォルト値 | 説明 |
|---|---|---|
| `DataRootDirectory` | `SystemPath.DefaultItemsFolderPath` | アイテムの保存先 |
| `AutoBackupRootDirectory` | `SystemPath.BackupFolderPath` | バックアップの保存先 |
| `RemoveOriginal` | `false` | 元ファイルを削除しない |
| `ShouldLinkToOriginal` | `false` | コピーを作成 |
| `AutoBackupInterval` | `5` | 5日ごと |
| `TreatEmptySupportedAvatarAsNone` | `false` | 空は空として扱う |
| `MaxDegreeOfParallelism` | `4` | 4並列 |
| `AutoChangeUnitypackagePath` | `true` | パスを自動変更 |
| `CheckForUpdate` | `true` | アップデートをチェック |
| `UpdateChannel` | `UpdateChannel.Stable` | 安定版チャンネル |

## 設定の更新

設定を更新するには、`RuntimeSettingsRepository.Update()`を使用します。

```csharp
// 現在の設定を取得
var currentSettings = app.RuntimeSettings;

// with式で一部を変更して更新
app.RuntimeSettingsRepository.Update(currentSettings with
{
    MaxDegreeOfParallelism = 8,
    AutoChangeUnitypackagePath = false
});
```

> **重要**: `RuntimeSettings`は`record`クラスなので、`with`式で簡単にコピーを作成できます。

### 設定変更時の自動処理

設定を変更すると、以下の処理が自動で行われます：

- `AutoBackupInterval`の変更: バックアップマネージャーのインターバル更新
- `AutoBackupRootDirectory`の変更: バックアップ先の更新

## 設定変更イベント

設定が変更されたときに通知を受けるには：

```csharp
app.RuntimeSettingsRepository.OnSettingsChanged += settings =>
{
    Console.WriteLine("設定が変更されました");
    Console.WriteLine($"  並列数: {settings.MaxDegreeOfParallelism}");
    Console.WriteLine($"  バックアップ間隔: {settings.AutoBackupInterval}日");
};
```

## 実践的な例

### 例1: 並列処理数の変更

```csharp
var settings = app.RuntimeSettings;

app.RuntimeSettingsRepository.Update(settings with
{
    MaxDegreeOfParallelism = Environment.ProcessorCount
});

Console.WriteLine($"並列処理数を {Environment.ProcessorCount} に変更しました");
```

### 例2: データ保存先の変更

```csharp
var settings = app.RuntimeSettings;

app.RuntimeSettingsRepository.Update(settings with
{
    DataRootDirectory = @"D:\VRChat\Avatar Data"
});

Console.WriteLine($"データ保存先を変更: {app.RuntimeSettings.DataRootDirectory}");
```

### 例3: バックアップ設定の変更

```csharp
var settings = app.RuntimeSettings;

app.RuntimeSettingsRepository.Update(settings with
{
    AutoBackupInterval = 7,  // 週次バックアップ
    AutoBackupRootDirectory = @"E:\Backup\AvatarExplorer"
});

Console.WriteLine("バックアップ設定を更新しました");
Console.WriteLine($"  間隔: {app.RuntimeSettings.AutoBackupInterval}日");
Console.WriteLine($"  保存先: {app.RuntimeSettings.AutoBackupRootDirectory}");
```

### 例4: インポート動作の変更

```csharp
var settings = app.RuntimeSettings;

app.RuntimeSettingsRepository.Update(settings with
{
    RemoveOriginal = true,          // インポート後に元ファイルを削除
    ShouldLinkToOriginal = false,   // コピーを作成
    AutoChangeUnitypackagePath = true  // Unitypackageパスを自動変更
});
```

### 例5: アップデート設定の変更

```csharp
using AvatarExplorer.Core.Models.Updates;

var settings = app.RuntimeSettings;

app.RuntimeSettingsRepository.Update(settings with
{
    CheckForUpdate = true,
    UpdateChannel = UpdateChannel.Beta  // ベータ版もチェック
});
```

## UpdateChannel

```csharp
public enum UpdateChannel
{
    Stable,  // 安定版のみ
    Beta     // ベータ版も含む
}
```

## 設定の使用例

### FileSystemServiceでの使用

```csharp
// AddPaths()でRuntimeSettingsが参照される
var result = await app.ItemRepository.AddPaths(
    "item:xxxxx",
    paths,
    shouldLinkToOriginal: app.RuntimeSettings.ShouldLinkToOriginal,
    removeOriginal: app.RuntimeSettings.RemoveOriginal
);
```

### ModifyUnitypackageFilePathsAsyncでの使用

```csharp
// changeUnitypackagePathをnullにすると、RuntimeSettingsから取得
var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(
    entries,
    changeUnitypackagePath: null  // RuntimeSettings.AutoChangeUnitypackagePathが使用される
);
```

### GetItemsFromAvatarでの使用

```csharp
// TreatEmptySupportedAvatarAsNoneが参照される
var items = app.ItemGroupService.GetItemsFromAvatar("item:avatar-id");
```

## 設定のバリデーション

設定を変更する際は、適切な値を設定してください。

```csharp
var settings = app.RuntimeSettings;

// 並列処理数のバリデーション
var newParallelism = Math.Clamp(requestedParallelism, 1, 16);

app.RuntimeSettingsRepository.Update(settings with
{
    MaxDegreeOfParallelism = newParallelism
});

// バックアップ間隔のバリデーション
var newInterval = Math.Clamp(requestedInterval, 1, 365);

app.RuntimeSettingsRepository.Update(settings with
{
    AutoBackupInterval = newInterval
});
```

## 次のステップ

[08 - データのインポート/エクスポート](./08-import-export.md) では、データのインポート・エクスポート機能について学びます。
