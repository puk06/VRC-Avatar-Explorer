# 03 - アイテムの管理

この章では、`ItemRepository`を使ったアイテムの作成・編集・削除について学びます。

## ItemRepositoryの取得

```csharp
var app = AvatarExplorerApp.Instance;
app.Initialize();

var itemRepo = app.ItemRepository;
```

## アイテムの作成

### ItemCreationContext

アイテムを作成するには、まず`ItemCreationContext`を作成します。

```csharp
using AvatarExplorer.Core.Models.Items;

var context = new ItemCreationContext
{
    Title = "サンプルアイテム",
    Author = "作者名",
    AuthorId = "author_subdomain",     // Boothのsubdomain（任意）
    BoothId = 12345678,                 // Booth商品ID（任意）
    ItemType = ItemType.Clothing,       // アイテムタイプ
    CustomCategory = "",                // カスタムカテゴリ名（任意）
    SupportedAvatars = new[]            // 対応アバターID一覧
    {
        "item:avatar-id-1",
        "item:avatar-id-2"
    },
    Tags = new[] { "タグ1", "タグ2" },  // タグ一覧
    ItemMemo = "アイテムの説明",
    ThumbnailUrl = "https://example.com/thumb.png",  // サムネイルURL（任意）
    IsHidden = false,                    // 非表示フラグ（任意）
    SkipIndirectCommonAvatarCheck = false // 共通素体チェックから除外するか（任意）
};
```

### Create() - アイテムの作成

```csharp
var newItem = await itemRepo.Create(context);

Console.WriteLine($"作成されたアイテム: {newItem.Title}");
Console.WriteLine($"Identifier: {newItem.Identifier}");
Console.WriteLine($"ID: {newItem.Id}");
```

`Create()`の内部処理：
1. 新しい`Item`インスタンスの作成
2. メタデータの設定
3. タイムスタンプの設定（作成日時・更新日時）
4. 対応アバター・タグ・非表示フラグ・共通素体チェック除外フラグの設定
5. サムネイルのダウンロード（`ThumbnailUrl`が指定されている場合）
6. データベースへの保存
7. BoothIdが指定されている場合、バックグラウンドでVariationHashをシード

### サムネイルのダウンロード

`ItemCreationContext.ThumbnailUrl`を指定すると、自動的にサムネイルがダウンロードされます。

```csharp
var context = new ItemCreationContext
{
    Title = "サムネイル付きアイテム",
    Author = "作者名",
    ItemType = ItemType.Avatar,
    ThumbnailUrl = "https://booth.px/img/..."  // サムネイルURL
};

var item = await itemRepo.Create(context);
// サムネイルが自動的にダウンロードされる
```

手動でサムネイルをダウンロードする場合：

```csharp
// ItemCreationContextからダウンロード
var downloaded = await context.FetchThumbnailAsync(destPath, overwrite: true);
```

## アイテムの取得

### GetAll() - 全アイテムの取得

```csharp
var allItems = itemRepo.GetAll();

foreach (var item in allItems)
{
    Console.WriteLine($"{item.Title} by {item.Author}");
}
```

### Get() - 特定アイテムの取得

```csharp
// Identifierからアイテムを取得
var item = itemRepo.Get("item:e10abf3d-cca7-4117-a5e9-52926a4f8990");

// またはIDのみで
var itemById = itemRepo.GetAll().Where(i => i.Id == "e10abf3d-cca7-4117-a5e9-52926a4f8990");

if (item != null)
{
    Console.WriteLine($"見つかった: {item.Title}");
}
```

## アイテムの編集

### ItemEditContext

アイテムを編集するには、`ItemEditContext`を使用します。

```csharp
using AvatarExplorer.Core.Models.Items;

var editContext = new ItemEditContext
{
    Title = "新しいタイトル",          // nullの場合は変更なし
    Author = "新しい作者名",
    AuthorId = "new_author_id",
    BoothId = 99999999,
    ItemType = ItemType.Accessory,     // カテゴリの変更
    CustomCategory = "カスタム名",
    ItemMemo = "更新された説明",
    ItemPath = @"D:\new\path",
    SupportedAvatars = new[] { "item:new-avatar-id" },
    ImplementedAvatars = new[] { "item:implemented-id" },
    Tags = new[] { "新しいタグ" },
    IsHidden = false,
    SkipIndirectCommonAvatarCheck = false, // 共通素体チェックから除外するか（nullの場合は変更なし）
    ThumbnailUrl = "https://new-thumbnail.url"  // 新しいサムネイル
};
```

### Update() - アイテムの更新

```csharp
var success = await itemRepo.Update("item:xxxxx", editContext);

if (success)
{
    Console.WriteLine("更新成功");
}
else
{
    Console.WriteLine("アイテムが見つかりません");
}
```

### 個別の更新メソッド

`Item`クラスの各`Update*`メソッドでも更新できますが、`Save()`を呼び出す必要があります。

```csharp
var item = itemRepo.Get("item:xxxxx");
if (item != null)
{
    item.UpdateTitle("新しいタイトル");
    item.UpdateAuthor("新しい作者");
    item.UpdateTags(new[] { "タグ1", "タグ2" });

    // 保存を忘れずに
    itemRepo.Save();
}
```

## アイテムの削除

### RemoveItem() - 安全な削除

**重要**: アイテムの削除には、必ず`ItemGroupService.RemoveItem()`を使用してください。

```csharp
// 正しい方法
app.ItemGroupService.RemoveItem("item:xxxxx");

// フォルダも一緒に削除する場合
app.ItemGroupService.RemoveItem("item:xxxxx", removeFolder: true);
```

`RemoveItem()`の内部処理：
1. 全アイテムから、削除するアイテムへの参照を削除
   - `SupportedAvatars`から削除
   - `ImplementedAvatars`から削除
2. 共通素体グループから参照を削除
3. アイテム自体を削除

### 単体での削除（非推奨）

`ItemRepository.Remove()`もありますが、関連データがクリーンアップされないため推奨されません。

```csharp
// 非推奨: 関連データがクリーンアップされない
itemRepo.Remove("item:xxxxx");
```

## パスの管理

### GetItemPath() - フルパスの取得

`Item.ItemPath`は相対パスの可能性があるため、フルパスを取得するには`GetItemPath()`を使用します。

```csharp
var item = itemRepo.Get("item:xxxxx");
var fullPath = item.GetItemPath();
Console.WriteLine($"フルパス: {fullPath}");
```

### AddPaths() - パスの追加

アイテムにファイル・フォルダパスを追加します。

```csharp
using AvatarExplorer.Core.Services.IO;

var paths = new List<ItemPathEntry>
{
    new ItemPathEntry
    {
        FileName = "package.zip",
        Path = @"C:\Downloads\package.zip",
        IsUrl = false
    },
    new ItemPathEntry
    {
        FileName = "online_asset",
        Path = "https://example.com/asset.zip",
        IsUrl = true
    }
};

var result = await itemRepo.AddPaths(
    "item:xxxxx",
    paths,
    shouldLinkToOriginal: false,  // true: フォルダだった場合は元フォルダへリンク、false: コピー
    removeOriginal: true          // true: アーカイブだった場合は展開後に元のファイルを削除
);

if (!result.IsError)
{
    Console.WriteLine("パスの追加成功");

    // アーカイブ展開 or フォルダコピーが発生した場合にパスが設定される
    Console.WriteLine($"展開先: {result.Value.ItemParentFolder}");

    // shouldLinkToOriginalがtrueで、パスがフォルダーだった場合はここに入ります
    Console.WriteLine($"展開されずにそのまま追加されたフォルダー: {string.Join(", ", result.Value.FolderPaths)}");
}
```

### RemovePath() - パスの削除

```csharp
await itemRepo.RemovePath(
    "item:xxxxx",
    @"D:\path\to\folder",
    deleteFolder: true  // true: フォルダも削除
);
```

### EnumerateItemFolders() - フォルダ一覧の取得

```csharp
var folders = itemRepo.EnumerateItemFolders("item:xxxxx");

foreach (var folder in folders)
{
    Console.WriteLine(folder);
}
```

### EnumerateItemFiles() - ファイル一覧の取得

```csharp
var files = itemRepo.EnumerateItemFiles("item:xxxxx");

foreach (var file in files)
{
    Console.WriteLine($"{file.FileName} ({file.Extension})");
    Console.WriteLine($"  パス: {file.FilePath}");
    Console.WriteLine($"  親フォルダ: {file.ParentFolderPath}");
}
```

## サムネイルの管理

### UpdateThumbnail() - サムネイルの更新

```csharp
var result = await itemRepo.UpdateThumbnail(
    "item:xxxxx",
    @"C:\path\to\thumbnail.png"
);

if (!result.IsError)
{
    Console.WriteLine("サムネイル更新成功");
}
```

### FetchThumbnailFromBooth() - Boothからサムネイルを取得

```csharp
var result = await itemRepo.FetchThumbnailFromBooth("item:xxxxx");

if (result.IsError)
{
    Console.WriteLine($"エラー: {result.Errors}");
}
```

## カテゴリの管理

### RenameCustomCategory() - カスタムカテゴリ名の変更

```csharp
itemRepo.RenameCustomCategory("旧カテゴリ名", "新カテゴリ名");
```

### MergeCategory() - カテゴリのマージ

```csharp
var sourceCategory = new ItemCategory(ItemType.Texture);
var targetCategory = new ItemCategory(ItemType.Clothing);

itemRepo.MergeCategory(sourceCategory, targetCategory);
// Textureカテゴリのアイテムが全てClothingカテゴリに移動
```

## タグの管理

### RenameTag() - タグ名の変更

```csharp
itemRepo.RenameTag("旧タグ", "新タグ");
```

### RemoveTag() - タグの削除

```csharp
itemRepo.RemoveTag("不要なタグ");
```

## CategorizeItems() - アイテムのカテゴリ分け

アイテムをカテゴリ別にグループ化します。

```csharp
var items = itemRepo.GetAll();
var categorized = ItemRepository.CategorizeItems(items);

foreach (var group in categorized)
{
    var category = ItemCategory.FromIdentifier(group.Key);
    Console.WriteLine($"カテゴリ: {category}");
    Console.WriteLine($"  件数: {group.Value.Count}");
}
```

## SearchItemFiles() - ファイルの検索

アイテム内のファイルを検索します。

```csharp
var results = itemRepo.SearchItemFiles("item:xxxxx", ".unitypackage");

foreach (var file in results)
{
    Console.WriteLine(file.FileName);
}
```

## ValidateAndAutoFixItemType() - ItemTypeの自動修正

データベースの整合性を保つために、ItemTypeを自動修正します。

> [!WARNING]
> CoreのVersionがv2.8.0より前は、カスタムカテゴリが移行されずに破壊されます。最新のmainブランチでは修正されています。

```csharp
// avatarExist: ユーザーがアバターを追加したことがあるかどうか（= ItemTypeが壊れていないかの判定に使用）
itemRepo.ValidateAndAutoFixItemType(avatarExist: true);
```

## 実践的な例

### 例1: Boothからアイテムを登録

```csharp
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.External.Booth;

var app = AvatarExplorerApp.Instance;
app.Initialize();

// Boothから商品情報を取得
var boothResult = await BoothService.Fetch("https://booth.pm/ja/items/12345678");
if (boothResult.IsError)
{
    Console.WriteLine("取得失敗");
    return;
}

var boothItem = boothResult.Value;

// アイテム作成コンテキストの作成
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
    IsHidden = false,
    SkipIndirectCommonAvatarCheck = false
};

// アイテムの作成
var newItem = await app.ItemRepository.Create(context);
Console.WriteLine($"作成完了: {newItem.Title}");
```

### 例2: アイテムの一括更新

```csharp
var items = app.ItemRepository.GetAll()
    .Where(i => i.Author == "特定の作者");

foreach (var item in items)
{
    var editContext = new ItemEditContext
    {
        Tags = item.Tags.Append("新タグ").ToArray()
    };

    await app.ItemRepository.Update(item.Identifier, editContext);
}
```

### 例3: アイテムの削除とクリーンアップ

```csharp
var itemToDelete = app.ItemRepository.Get("item:xxxxx");
if (itemToDelete != null)
{
    // ItemGroupServiceを使って安全に削除
    app.ItemGroupService.RemoveItem(itemToDelete.Identifier, removeFolder: true);
    Console.WriteLine($"{itemToDelete.Title} を削除しました");
}
```

## 次のステップ

[04 - 共通素体・仮アバターの管理](./04-avatar-management.md) では、共通素体グループと仮アバターの管理について学びます。
