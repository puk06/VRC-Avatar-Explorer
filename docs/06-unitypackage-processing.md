# 06 - Unitypackageの処理

この章では、`FileSystemService`を使ったUnitypackageの処理について学びます。

## Unitypackageパスの変更

AvatarExplorerには、Unitypackageのインポートパスを自動で変更する機能があります。

**変更前**: `Assets/作者名/アイテム`
**変更後**: `Assets/カテゴリ名/作者名/アイテム`

## ModifyUnitypackageFilePathsAsync()

### 基本的な使い方

```csharp
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Models.External;

var entries = new List<UnitypackageImportEntry>
{
    new UnitypackageImportEntry
    {
        FilePath = @"C:\path\to\package1.unitypackage",
        CategoryDisplayName = "衣装"
    },
    new UnitypackageImportEntry
    {
        FilePath = @"C:\path\to\package2.unitypackage",
        CategoryDisplayName = "アクセサリー"
    }
};

var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(entries);

if (result.IsError)
{
    Console.WriteLine("エラー発生");
}
else
{
    Console.WriteLine($"新しいUnitypackage: {result.ModifiedUnitypackagePath}");
}
```

### UnitypackageImportEntry

```csharp
public class UnitypackageImportEntry
{
    public string FilePath { get; init; }           // Unitypackageのパス
    public string CategoryDisplayName { get; init; } // カテゴリ名
}
```

### パラメータ

```csharp
var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(
    entries,
    changeUnitypackagePath: true,  // パスを変更するか（nullの場合はRuntimeSettingsから取得）
    reportProgress: async (status, progress) =>
    {
        Console.WriteLine($"{status}: {progress}%");
    }
);
```

### ModifiedUnitypackagesResult

```csharp
public class ModifiedUnitypackagesResult
{
    public bool IsError { get; set; }                    // エラーが発生したか
    public string? ModifiedUnitypackagePath { get; set; } // 作成されたUnitypackageのパス
    public List<string> Success { get; }                 // 成功した入力ファイル
    public List<string> Failed { get; }                  // 失敗した入力ファイル
    public bool ContainsScripts { get; set; }            // C#スクリプトを含むか
    public bool IsNotModified { get; set; }              // 変更なし（元ファイルと同じ）
}
```

## 複数のUnitypackageをまとめる

`ModifyUnitypackageFilePathsAsync()`は、複数のUnitypackageを1つにまとめる機能もあります。

```csharp
// 複数のUnitypackageを1つにまとめる（カテゴリは挟まない）
var entries = new List<UnitypackageImportEntry>
{
    new UnitypackageImportEntry
    {
        FilePath = @"C:\path\to\body.unitypackage",
        CategoryDisplayName = ""  // 空の場合はパス変更なし
    },
    new UnitypackageImportEntry
    {
        FilePath = @"C:\path\to\clothes.unitypackage",
        CategoryDisplayName = ""
    }
};

var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(
    entries,
    changeUnitypackagePath: false  // パス変更を無効化
);

// 2つのUnitypackageが1つにまとめられる
Console.WriteLine($"統合されたパッケージ: {result.ModifiedUnitypackagePath}");
```

## パス変更の仕組み

パス変更は、Unitypackage内の`pathname`ファイルを書き換えることで実現されています。

```
変更前: Assets/作者名/アイテム名/ファイル
変更後: Assets/カテゴリ名/作者名/アイテム名/ファイル
```

> **注意**: `Assets`で始まるパスのみが変更対象です。`Packages`などで始まるパスは変更されません。

## スクリプトの検出

UnitypackageにC#スクリプト（.csファイル）が含まれている場合、`ContainsScripts`が`true`になります。

```csharp
var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(entries);

if (result.ContainsScripts)
{
    Console.WriteLine("警告: このパッケージにはC#スクリプトが含まれています");
    Console.WriteLine("パス変更により、スクリプトの参照が壊れる可能性があります");
}
```

## Unitypackageのpathname一覧取得

Unitypackageに含まれるpathname一覧を取得します。

```csharp
var pathnamesResult = await FileSystemService.GetUnitypackagePathnamesAsync(
    @"C:\path\to\package.unitypackage"
);

if (pathnamesResult.IsError)
{
    Console.WriteLine("取得失敗");
    return;
}

foreach (var pathname in pathnamesResult.Value)
{
    Console.WriteLine(pathname);
    // Assets/AuthorName/ItemName/file.meta
    // Assets/AuthorName/ItemName/prefab.prefab
    // ...
}
```

## Unitypackageからアセットを抽出

特定のpathnameのアセットを抽出します。

```csharp
var extractResult = await FileSystemService.ExtractUnitypackageAssetAsync(
    unitypackagePath: @"C:\path\to\package.unitypackage",
    pathname: "Assets/AuthorName/ItemName/prefab.prefab",
    destinationFolderPath: @"C:\extract\destination"
);

if (extractResult.IsError)
{
    Console.WriteLine("抽出失敗");
    return;
}

Console.WriteLine($"抽出先: {extractResult.Value}");
```

## 実践的な例

### 例1: カテゴリを指定してUnitypackageをインポート準備

```csharp
using AvatarExplorer.Core.Models.Items;

// アイテム情報を取得
var item = app.ItemRepository.Get("item:xxxxx");
if (item == null) return;

// カテゴリ名を取得
// item.Category.ToString()で、ItemTypeの場合はLocalizationKey、CustomCategoryの場合はそのままの名前が取得できる
var categoryName = item.Category.ToString();

// 必要に応じてローカライズ（日本語に変換）
var localizedCategoryName = item.Category.Type switch
{
    ItemType.Clothing => "衣装",
    ItemType.Accessory => "アクセサリー",
    ItemType.Texture => "テクスチャ",
    ItemType.Gimmick => "ギミック",
    ItemType.HairStyle => "髪型",
    ItemType.Custom => item.Category.CustomCategory, // カスタムカテゴリはそのまま
    _ => "その他"
};

// Unitypackageの準備
var entries = new List<UnitypackageImportEntry>
{
    new UnitypackageImportEntry
    {
        FilePath = @"C:\Downloads\item.unitypackage",
        CategoryDisplayName = localizedCategoryName
    }
};

var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(entries);

if (!result.IsError && result.ModifiedUnitypackagePath != null)
{
    Console.WriteLine($"準備完了: {result.ModifiedUnitypackagePath}");
    Console.WriteLine("このファイルをUnityにインポートしてください");
    
    // スクリプトが含まれている場合は警告
    if (result.ContainsScripts)
    {
        Console.WriteLine("注意: C#スクリプトが含まれています。参照が壊れる可能性があります。");
    }
}
```

### 例2: 複数アイテムをまとめてインポート準備

```csharp
// ナビゲーションで選択されたアイテムのファイルを収集
var navigation = app.ItemNavigationService;
var viewItems = navigation.GetCurrentSelectionView();

var entries = new List<UnitypackageImportEntry>();

foreach (var viewItem in viewItems)
{
    if (viewItem is Item item)
    {
        // アイテムのUnitypackageファイルを検索
        var files = app.ItemRepository.EnumerateItemFiles(item.Identifier);
        var unitypackages = files.Where(f => 
            f.Extension.Equals("unitypackage", StringComparison.OrdinalIgnoreCase));
        
        var categoryName = item.Category.ToString();
        
        foreach (var up in unitypackages)
        {
            entries.Add(new UnitypackageImportEntry
            {
                FilePath = up.FilePath,
                CategoryDisplayName = categoryName
            });
        }
    }
}

if (entries.Count > 0)
{
    var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(
        entries,
        reportProgress: async (status, progress) =>
        {
            Console.Write($"\r{status}: {progress}%");
        }
    );

    Console.WriteLine();

    if (!result.IsError && result.ModifiedUnitypackagePath != null)
    {
        Console.WriteLine($"統合パッケージ作成完了: {result.ModifiedUnitypackagePath}");
        Console.WriteLine($"成功: {result.Success.Count}件, 失敗: {result.Failed.Count}件");
    }
}
else
{
    Console.WriteLine("Unitypackageが見つかりませんでした");
}
```

### 例3: パス変更なしでマージのみ

```csharp
// パス変更は行わず、複数のUnitypackageを1つにまとめるだけ
var entries = new List<UnitypackageImportEntry>
{
    new UnitypackageImportEntry { FilePath = path1, CategoryDisplayName = "" },
    new UnitypackageImportEntry { FilePath = path2, CategoryDisplayName = "" },
    new UnitypackageImportEntry { FilePath = path3, CategoryDisplayName = "" }
};

var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(
    entries,
    changeUnitypackagePath: false
);

if (!result.IsError)
{
    Console.WriteLine($"マージ完了: {result.ModifiedUnitypackagePath}");
}
```

## RuntimeSettingsとの連携

`changeUnitypackagePath`パラメータに`null`を指定すると、`RuntimeSettings.AutoChangeUnitypackagePath`の値が使用されます。

```csharp
// RuntimeSettingsの設定に従う
var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(
    entries,
    changeUnitypackagePath: null  // RuntimeSettingsから取得
);
```

## 次のステップ

[07 - 設定の管理](./07-runtime-settings.md) では、RuntimeSettingsの読み書きについて学びます。
