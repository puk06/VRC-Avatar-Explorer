# 08 - データのインポート/エクスポート

この章では、`ItemGroupService`を使ったデータのインポート・エクスポートについて学びます。

## エクスポート

### Export() - データのエクスポート

```csharp
using AvatarExplorer.Core.Models.External;

var request = new ExportRequest
{
    ExportType = DataExportType.Csv,
    FolderPath = @"C:\export\destination",
    ItemTypeLocalizer = type => new ValueTask<string?>(type.ToString()),  // カテゴリ名のローカライズ関数
    IncludeCommonToSupported = true,                                       // 共通素体を含めるか
    ReportProgress = async progress => Console.WriteLine($"{progress.Item1}: {progress.Item2}%")
};

var result = await app.ItemGroupService.Export(request);

if (result.IsError)
{
    Console.WriteLine("エクスポート失敗");
}
else
{
    Console.WriteLine("エクスポート成功");
}
```

### DataExportType

```csharp
public enum DataExportType
{
    None,        // なし
    Csv,         // CSV形式
    KonoAsset    // KonoAsset形式
}
```

### ExportRequest

```csharp
public class ExportRequest
{
    public DataExportType ExportType { get; set; }
    public string FolderPath { get; set; }
    public bool IncludeCommonToSupported { get; set; }
    public Func<ItemType, ValueTask<string?>>? ItemTypeLocalizer { get; set; }
    public Func<(string, int), Task>? ReportProgress { get; set; }
}
```

### パラメータ詳細

| プロパティ | 型 | 説明 |
|---|---|---|
| `ExportType` | `DataExportType` | エクスポート形式 |
| `FolderPath` | `string` | 出力先フォルダ |
| `IncludeCommonToSupported` | `bool` | 共通素体を対応アバターに展開するか |
| `ItemTypeLocalizer` | `Func<ItemType, ValueTask<string?>>?` | カテゴリ名をローカライズする関数 |
| `ReportProgress` | `Func<(string, int), Task>?` | 進捗報告コールバック |

## インポート

### Import() - データのインポート

```csharp
using AvatarExplorer.Core.Models.External;

var request = new ImportRequest
{
    ImportType = DataImportType.V1 | DataImportType.Items,
    DataFolderPath = @"C:\import\source",
    CopyAssetData = true,
    ReportProgress = async progress => Console.WriteLine($"{progress.Item1}: {progress.Item2}%");
};

var result = await app.ItemGroupService.Import(request);

if (result.IsError)
{
    Console.WriteLine("インポート失敗");
}
else
{
    Console.WriteLine("インポート成功");
}
```

### DataImportType

```csharp
[Flags]
public enum DataImportType
{
    None = 0,
    V1 = 1,           // AvatarExplorer V1形式
    KonoAsset = 2,    // KonoAsset形式
    Folder = 4,       // フォルダからインポート
    SourceMask = V1 | KonoAsset | Folder,  // ソース指定のマスク
    Items = 8,        // アイテムをインポート
    Thumbnails = 16   // サムネイルをインポート
}
```

### ImportRequest

```csharp
public class ImportRequest
{
    public DataImportType ImportType { get; set; }      // インポートタイプ
    public string DataFolderPath { get; set; }          // データフォルダのパス
    public bool CopyAssetData { get; set; }             // アセットデータをコピーするか
    public Func<(string, int), Task>? ReportProgress { get; set; }  // 進捗報告
}
```

### インポートの組み合わせ

`DataImportType`はFlags列挙型なので、組み合わせが可能です。

```csharp
// V1形式からアイテムとサムネイルをインポート
var request = new ImportRequest
{
    ImportType = DataImportType.V1 | DataImportType.Items | DataImportType.Thumbnails,
    DataFolderPath = @"C:\import\source",
    CopyAssetData = true
};

// KonoAsset形式からインポート
var request2 = new ImportRequest
{
    ImportType = DataImportType.KonoAsset | DataImportType.Items,
    DataFolderPath = @"C:\konoasset\data",
    CopyAssetData = false  // リンクを作成
};

// フォルダから直接インポート
var request3 = new ImportRequest
{
    ImportType = DataImportType.Folder | DataImportType.Items,
    DataFolderPath = @"C:\avatars\folder",
    CopyAssetData = true
};
```

## 実践的な例

### 例1: CSVエクスポート

```csharp
// カテゴリのローカライズ関数
ValueTask<string?> LocalizeItemType(ItemType type)
{
    var localized = type switch
    {
        ItemType.Avatar => "アバター",
        ItemType.Clothing => "衣装",
        ItemType.Texture => "テクスチャ",
        ItemType.Gimmick => "ギミック",
        ItemType.Accessory => "アクセサリー",
        ItemType.HairStyle => "髪型",
        ItemType.Animation => "アニメーション",
        ItemType.Tool => "ツール",
        ItemType.Shader => "シェーダー",
        _ => type.ToString()
    };
    return new ValueTask<string?>(localized);
}

var request = new ExportRequest
{
    ExportType = DataExportType.Csv,
    FolderPath = @"C:\export",
    ItemTypeLocalizer = LocalizeItemType,
    IncludeCommonToSupported = true,
    ReportProgress = async (status, progress) =>
    {
        Console.Write($"\r{status}: {progress}%");
    }
};

var result = await app.ItemGroupService.Export(request);

Console.WriteLine();

if (!result.IsError)
{
    Console.WriteLine("CSVエクスポート完了");
}
```

### 例2: KonoAssetからのインポート

```csharp
var request = new ImportRequest
{
    ImportType = DataImportType.KonoAsset | DataImportType.Items | DataImportType.Thumbnails,
    DataFolderPath = @"C:\Users\username\AppData\Local\KonoAsset\data",
    CopyAssetData = true,
    ReportProgress = async (status, progress) =>
    {
        Console.WriteLine($"[{progress}%] {status}");
    }
};

var result = await app.ItemGroupService.Import(request);

if (result.IsError)
{
    Console.WriteLine($"インポート失敗: {result.Errors}");
}
else
{
    Console.WriteLine("インポート完了");

    // インポート後のアイテム数を確認
    var itemCount = app.ItemRepository.GetAll().Count;
    Console.WriteLine($"現在のアイテム数: {itemCount}");
}
```

### 例3: V1からのマイグレーション

```csharp
// AvatarExplorer V1からのマイグレーション
var request = new ImportRequest
{
    ImportType = DataImportType.V1 | DataImportType.Items | DataImportType.Thumbnails,
    DataFolderPath = @"C:\AvatarExplorer\v1\data",
    CopyAssetData = true,
    ReportProgress = async (status, progress) =>
    {
        Console.Write($"\rマイグレーション中... {progress}%");
    }
};

Console.WriteLine("V1からのマイグレーションを開始します...");

var result = await app.ItemGroupService.Import(request);

Console.WriteLine();

if (!result.IsError)
{
    Console.WriteLine("マイグレーション完了");

    // データベースの整合性を確認
    app.ItemRepository.ValidateAndAutoFixItemType(avatarExist: true);
    Console.WriteLine("データベースの整合性チェック完了");
}
```

### 例4: バックアップからの復元

```csharp
// バックアップフォルダからインポート
var backupFolder = @"E:\Backup\AvatarExplorer\2024-01-01";

if (!Directory.Exists(backupFolder))
{
    Console.WriteLine("バックアップフォルダが見つかりません");
    return;
}

var request = new ImportRequest
{
    ImportType = DataImportType.V1 | DataImportType.Items | DataImportType.Thumbnails,
    DataFolderPath = backupFolder,
    CopyAssetData = true
};

var result = await app.ItemGroupService.Import(request);

if (!result.IsError)
{
    Console.WriteLine("バックアップからの復元完了");
}
```

## ExportContextとExportRequest

内部で使用される型について：

### ExportContext

内部でエクスポート処理に渡されるデータコンテキストです。

```csharp
public class ExportContext
{
    public required IEnumerable<Item> Items { get; init; }
    public required IEnumerable<CommonAvatar> CommonAvatars { get; init; }
    public required IEnumerable<TempAvatar> TempAvatars { get; init; }
    public required RuntimeSettings RuntimeSettings { get; init; }
}
```

## 次のステップ

[09 - 検索機能](./09-search.md) では、検索クエリとフィルタリングについて学びます。
