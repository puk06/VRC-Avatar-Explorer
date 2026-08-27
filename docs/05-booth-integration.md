# 05 - Booth連携

この章では、`BoothService`を使ったBooth APIとの連携について学びます。

## BoothServiceの基本

`BoothService`は、Boothの商品情報を取得するためのサービスです。

```csharp
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Models.External.Booth;

var app = AvatarExplorerApp.Instance;
app.Initialize();
```

## Fetch() - 商品情報の取得

### URLから取得

```csharp
var result = await BoothService.Fetch("https://booth.pm/ja/items/12345678");

if (result.IsError)
{
    Console.WriteLine($"エラー: {result.Errors}");
    return;
}

var boothItem = result.Value;
Console.WriteLine($"商品名: {boothItem.Title}");
Console.WriteLine($"ショップ: {boothItem.Shop.Name}");
Console.WriteLine($"カテゴリ: {boothItem.Category.Name}");
```

### 推定カテゴリ

BoothServiceは、商品タイトルとカテゴリからアイテムタイプを推定します。

```csharp
Console.WriteLine($"推定カテゴリ: {boothItem.EstimatedCategory.Type}");
// ItemType.Avatar, ItemType.Clothing, etc.
```

### パラメータ

```csharp
var result = await BoothService.Fetch(
    boothUrl: "https://booth.pm/ja/items/12345678",
    waitCooldown: true,       // APIクールダウンを待機するか
    includeVariations: false  // バリエーション情報を含めるか
);
```

## APIクールダウン

Booth APIには3秒のクールダウンがあります。

```csharp
// クールダウン中かどうかを確認
if (BoothService.IsApiCooldownNow)
{
    Console.WriteLine("APIクールダウン中です");
}

// Fetch()は自動的にクールダウンを待機しますが、
// waitCooldown: falseを指定すると、クールダウン中の場合はエラーを返します
var result = await BoothService.Fetch(url, waitCooldown: false);
if (result.IsError)
{
    Console.WriteLine("クールダウン中のため取得失敗");
}
```

## BoothItemモデル

```csharp
public record BoothItem
{
    public string Title { get; }              // 商品名
    public ShopInfo Shop { get; }             // ショップ情報
    public int BoothId { get; }               // 商品ID
    public ImageInfo[] Thumbnails { get; }    // サムネイル画像
    public Variation[] Variations { get; }    // バリエーション
    public CategoryInfo Category { get; }     // カテゴリ情報

    // AvatarExplorerが追加したプロパティ
    public ItemCategory EstimatedCategory { get; }  // 推定カテゴリ
    public string ThumbnailUrl { get; }             // 最初のサムネイルURL
}
```

### ShopInfo

```csharp
public record ShopInfo
{
    public string Name { get; }          // ショップ名
    public string Id { get; }            // ショップのsubdomain
    public string ThumbnailUrl { get; }  // ショップのサムネイルURL
}
```

### CategoryInfo

```csharp
public record CategoryInfo
{
    public string Name { get; }  // カテゴリ名
}
```

### ImageInfo

```csharp
public record ImageInfo
{
    public string Original { get; }  // 元画像のURL
}
```

### Variation

```csharp
public record Variation
{
    public int Id { get; }                    // バリエーションID
    public string? Name { get; }              // バリエーション名
    public int Price { get; }                 // 価格
    public string Type { get; }               // タイプ
    public Downloadables[] Downloadables { get; }  // ダウンロード可能ファイル
}
```

### Downloadables

```csharp
public record Downloadables
{
    public string FileName { get; }       // ファイル名
    public string FileExtension { get; }  // 拡張子
    public string FileSize { get; }       // ファイルサイズ
    public string Name { get; }           // 表示名
    public string CreatedAt { get; }      // 作成日時
    public string UpdatedAt { get; }      // 更新日時
    public int DisplayOrder { get; }      // 表示順序
}
```

## Boothからアイテムを作成

### 基本的な流れ

```csharp
using AvatarExplorer.Core.Models.Items;

// 1. Boothから商品情報を取得
var boothResult = await BoothService.Fetch("https://booth.pm/ja/items/12345678");
if (boothResult.IsError)
{
    Console.WriteLine("取得失敗");
    return;
}

var boothItem = boothResult.Value;

// 2. ItemCreationContextの作成
var context = new ItemCreationContext
{
    Title = boothItem.Title,
    Author = boothItem.Shop.Name,
    AuthorId = boothItem.Shop.Id,
    BoothId = boothItem.BoothId,
    ItemType = boothItem.EstimatedCategory.Type,
    ThumbnailUrl = boothItem.ThumbnailUrl,
    SupportedAvatars = Array.Empty<string>(),
    Tags = Array.Empty<string>(),
    ItemMemo = ""
};

// 3. アイテムの作成
var newItem = await app.ItemRepository.Create(context);
Console.WriteLine($"作成完了: {newItem.Title}");
```

### バリエーション情報を含める

```csharp
var boothResult = await BoothService.Fetch(
    "https://booth.pm/ja/items/12345678",
    includeVariations: true
);

if (!boothResult.IsError)
{
    var boothItem = boothResult.Value;

    // バリエーション一覧の表示
    foreach (var variation in boothItem.Variations)
    {
        Console.WriteLine($"バリエーション: {variation.Name}");
        Console.WriteLine($"  価格: {variation.Price}円");
        
        foreach (var downloadable in variation.Downloadables)
        {
            Console.WriteLine($"  ファイル: {downloadable.FileName}");
        }
    }
}
```

## Boothリンクの生成

アイテムからBoothへのリンクを生成します。

```csharp
var item = app.ItemRepository.Get("item:xxxxx");

if (item != null)
{
    // 言語コードを指定してリンクを生成
    var boothLink = item.GetBoothLink("ja");
    Console.WriteLine($"Boothリンク: {boothLink}");

    // AuthorIdがある場合: https://xxx.booth.pm/items/12345678
    // AuthorIdがない場合: https://booth.pm/ja/items/12345678
}
```

## サムネイルの取得

### Boothからサムネイルを再取得

```csharp
var result = await app.ItemRepository.FetchThumbnailFromBooth("item:xxxxx");

if (result.IsError)
{
    Console.WriteLine($"エラー: {result.Errors}");
}
else
{
    Console.WriteLine("サムネイル更新成功");
}
```

## 実践的な例

### 例1: Booth URLから一括登録

```csharp
var boothUrls = new[]
{
    "https://booth.pm/ja/items/11111111",
    "https://booth.pm/ja/items/22222222",
    "https://booth.pm/ja/items/33333333"
};

foreach (var url in boothUrls)
{
    var result = await BoothService.Fetch(url);
    if (result.IsError)
    {
        Console.WriteLine($"取得失敗: {url}");
        continue;
    }

    var boothItem = result.Value;
    
    // 既に登録済みかチェック
    var existing = app.ItemRepository.GetAll()
        .FirstOrDefault(i => i.BoothId == boothItem.BoothId);

    if (existing != null)
    {
        Console.WriteLine($"既に登録済み: {existing.Title}");
        continue;
    }

    // 新規登録
    var context = new ItemCreationContext
    {
        Title = boothItem.Title,
        Author = boothItem.Shop.Name,
        AuthorId = boothItem.Shop.Id,
        BoothId = boothItem.BoothId,
        ItemType = boothItem.EstimatedCategory.Type,
        ThumbnailUrl = boothItem.ThumbnailUrl
    };

    var newItem = await app.ItemRepository.Create(context);
    Console.WriteLine($"登録完了: {newItem.Title}");
}
```

### 例2: アイテム情報の更新

```csharp
var item = app.ItemRepository.Get("item:xxxxx");
if (item == null || item.BoothId == -1)
{
    Console.WriteLine("Booth IDが設定されていません");
    return;
}

// Boothから最新情報を取得
var result = await BoothService.Fetch(item.BoothId.ToString());
if (result.IsError)
{
    Console.WriteLine("取得失敗");
    return;
}

var boothItem = result.Value;

// 情報を更新
var editContext = new ItemEditContext
{
    Title = boothItem.Title,
    Author = boothItem.Shop.Name,
    AuthorId = boothItem.Shop.Id,
    ItemType = boothItem.EstimatedCategory.Type
};

await app.ItemRepository.Update(item.Identifier, editContext);

// サムネイルも更新
await app.ItemRepository.FetchThumbnailFromBooth(item.Identifier);

Console.WriteLine("更新完了");
```

### 例3: アイテムタイプの手動修正

Boothの推定が間違っている場合：

```csharp
var boothResult = await BoothService.Fetch(url);
var boothItem = boothResult.Value;

// 推定カテゴリを確認
Console.WriteLine($"推定: {boothItem.EstimatedCategory.Type}");

// 手動で修正
var context = new ItemCreationContext
{
    Title = boothItem.Title,
    Author = boothItem.Shop.Name,
    BoothId = boothItem.BoothId,
    ItemType = ItemType.Tool,  // 手動で指定
    ThumbnailUrl = boothItem.ThumbnailUrl
};
```

## カテゴリ推定の仕組み

BoothServiceは、以下の情報からアイテムタイプを推定します：

1. **カテゴリマッピング** - Boothのカテゴリ名から推定
2. **タイトルマッピング** - タイトルに含まれるキーワードから推定

タイトルマッピングが優先されます。

## 次のステップ

[06 - Unitypackageの処理](./06-unitypackage-processing.md) では、Unitypackageのパス変更やマージ機能について学びます。
