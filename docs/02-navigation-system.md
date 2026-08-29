# 02 - ナビゲーションシステム

この章では、`ItemNavigationService`を使ったナビゲーションの仕組みについて詳しく学びます。

## ナビゲーションの概念

AvatarExplorerのナビゲーションは、以下のような階層構造を持っています：

```
ルート
├── アバター選択 (avatar:...)
│   └── カテゴリ選択 (type:... / custom:...)
│       └── アイテム選択 (item:...)
│           └── フォルダ選択 (folder:...)
│               └── 拡張子選択 (extension:...)
│                   └── ファイル選択 (file:...)
└── 作者選択 (author:...)
    └── (同上)
```

## ItemNavigationServiceの取得

```csharp
var app = AvatarExplorerApp.Instance;
app.Initialize();

var navigation = app.ItemNavigationService;
```

## Identifierプレフィックス

`ItemNavigationService`では、以下のプレフィックス定数が定義されています：

```csharp
public const string AvatarPrefix = "avatar";       // アバター
public const string AuthorPrefix = "author";       // 作者
public const string TypePrefix = "type";           // 組み込みタイプカテゴリ
public const string CustomPrefix = "custom";       // カスタムカテゴリ
public const string ItemPrefix = "item";           // アイテム
public const string FolderPrefix = "folder";       // フォルダ
public const string ExtensionPrefix = "extension"; // 拡張子
public const string FilePrefix = "file";           // ファイル
```

## GetCurrentSelectionView()

現在の選択状態で表示可能なアイテム一覧を取得します。

```csharp
var viewItems = navigation.GetCurrentSelectionView();

foreach (var item in viewItems)
{
    Console.WriteLine($"{item.Identifier}: {item.GetType().Name}");
}
```

### 状態ごとの戻り値

| 状態 | 戻り値の型 | 内容 |
|---|---|---|
| 初期状態 | `Item[]` | 全アイテム（非表示を除く） |
| アバター選択後 | `Folder[]` | カテゴリ別フォルダ |
| 作者選択後 | `Folder[]` | カテゴリ別フォルダ |
| カテゴリ選択後 | `Item[]` | そのカテゴリのアイテム |
| アイテム選択後 | `Folder[]` | アイテム内のフォルダ |
| フォルダ選択後 | `Folder[]` | 拡張子別フォルダ |
| 拡張子選択後 | `ItemFile[]` | その拡張子のファイル |

## Select() - 選択操作

Identifierを指定して選択を行います。

```csharp
// アバターを選択
navigation.Select("avatar:item:e10abf3d-cca7-4117-a5e9-52926a4f8990");

// カテゴリを選択
navigation.Select("type:2"); // = new ItemCategory(ItemType.Clothing).Identifier

// アイテムを選択
navigation.Select("item:91517db1-eaf3-4137-914f-551cf3669692");

// フォルダを選択
navigation.Select("folder:850F5169");

// 拡張子を選択
navigation.Select("extension:2"); // .unitypackage (数字の部分は、(int)ItemFileCategoryType.Unitypackageと同じです)

// ファイルを選択（イベントが発火）
navigation.Select("file:a1b2c3d4...");
```

### Select()の戻り値

`Select()`は`Guid?`を返します。ファイル選択の場合は`null`が返ります。

この`Guid`は、AvatarExplorer.UIではナビゲーション時のページやスクロール位置の保存に使用されています。

```csharp
var result = navigation.Select("item:xxxxx");
if (result == null)
{
    // ファイルが選択された（FileOpenRequestedイベントで処理）
}
else
{
    // 通常の選択
}
```

## FileOpenRequestedイベント

ファイルが選択されると、`FileOpenRequested`イベントが発火します。

```csharp
navigation.FileOpenRequested += path =>
{
    Console.WriteLine($"ファイルが選択されました: {path}");
    // ここでファイルを開く処理を行う
    // 例: Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
};
```

このイベントには、選択されたファイルの**フルパス**が渡されます。

## パスの解決

フォルダやファイルのIdentifierには、パスのハッシュ値が含まれています。これを元のパスに戻すには`ResolvePath()`を使用します。

```csharp
var path = navigation.ResolvePath("folder:850F5169");
Console.WriteLine(path);
// 出力: D:\VRChat\Avatar Explorer Data\...
```

> **注意**: `ResolvePath()`は、一度選択操作が行われた後にのみ有効です。パスキャッシュが構築されるためです。

### Folderクラスからパスを取得

`Folder`クラスの`Path`プロパティからもフルパスを取得できます。

```csharp
var viewItems = navigation.GetCurrentSelectionView();
var folder = viewItems.OfType<Folder>().FirstOrDefault();

if (folder?.Path != null)
{
    Console.WriteLine($"フォルダパス: {folder.Path}");
}
```

## Undo - 一つ前に戻る

選択を一つ前の状態に戻します。

```csharp
var previousState = navigation.Undo();
if (previousState != null)
{
    Console.WriteLine($"前の状態: {previousState.Value}");
}
```

## Clear - 全選択を解除

選択状態をすべてクリアして、ルートに戻ります。

```csharp
navigation.Clear();
```

## PopToState - 特定の状態まで戻る

指定した状態まで一気に戻ります。

```csharp
navigation.PopToState("avatar:item:xxxxx");
```

## 現在の状態を取得

### CurrentState

現在の選択状態を取得します。

```csharp
var currentState = navigation.CurrentState;
if (currentState != null)
{
    Console.WriteLine($"現在の状態: {currentState.Value}");
}
```

### GetCurrentSelectionNodes()

選択履歴をすべて取得します。

```csharp
var nodes = navigation.GetCurrentSelectionNodes();
foreach (var node in nodes)
{
    Console.WriteLine($"履歴: {node.Value}");
}
```

### GetCurrentAvatarId()

現在選択されているアバターのIDを取得します。

```csharp
var avatarId = navigation.GetCurrentAvatarId();
if (avatarId != null)
{
    Console.WriteLine($"選択中のアバター: {avatarId}");
}
```

### GetCurrentItemId()

現在選択されているアイテムのIDを取得します。

```csharp
var itemId = navigation.GetCurrentItemId();
if (itemId != null)
{
    Console.WriteLine($"選択中のアイテム: {itemId}");
}
```

## QueryFilters - サイドパネル用フィルタ

アバター、作者、カテゴリの一覧（フィルタ用）を取得するには、`ItemGroupService.GetQueryFilters()`を使用します。

```csharp
// アバター一覧
var avatars = app.ItemGroupService.GetQueryFilters(QueryType.Avatar);

// 作者一覧
var authors = app.ItemGroupService.GetQueryFilters(QueryType.Author);

// カテゴリ一覧
var categories = app.ItemGroupService.GetQueryFilters(QueryType.Category);
```

### QueryType

```csharp
public enum QueryType
{
    Avatar,    // アバター一覧（共通素体・仮アバター含む）
    Author,    // 作者一覧
    Category   // カテゴリ一覧
}
```

### 使用例

```csharp
// アバター一覧の表示
var avatars = app.ItemGroupService.GetQueryFilters(QueryType.Avatar);
foreach (var avatar in avatars)
{
    Console.WriteLine(avatar.Identifier);
    // avatar:item:xxxxx (通常アイテム)
    // avatar:commonavatar:xxxxx (共通素体)
    // avatar:tempavatar:xxxxx (仮アバター)
}

// アバターを選択してフィルタリング
navigation.Select(avatars[0].Identifier);
```

## 実践的なナビゲーション例

### 例1: アイテム一覧からファイルを開くまで

```csharp
var navigation = app.ItemNavigationService;

// 1. 初期状態でアイテム一覧を取得
var items = navigation.GetCurrentSelectionView();
Console.WriteLine("=== アイテム一覧 ===");
foreach (var item in items.Take(5))
{
    if (item is Item i)
        Console.WriteLine($"  {i.Title} ({i.Identifier})");
}

// 2. 最初のアイテムを選択
var firstItem = items.First();
navigation.Select(firstItem.Identifier);

// 3. フォルダ一覧を取得
var folders = navigation.GetCurrentSelectionView();
Console.WriteLine("\n=== フォルダ一覧 ===");
foreach (var folder in folders)
{
    if (folder is Folder f)
        Console.WriteLine($"  {f.Title} ({f.Identifier})");
}

// 4. 最初のフォルダを選択
var firstFolder = folders.First();
navigation.Select(firstFolder.Identifier);

// 5. 拡張子一覧を取得
var extensions = navigation.GetCurrentSelectionView();
Console.WriteLine("\n=== 拡張子一覧 ===");
foreach (var ext in extensions)
{
    if (ext is Folder ef)
        Console.WriteLine($"  {ef.Title} ({ef.Identifier})");
}

// 6. ファイル選択イベントを購読
navigation.FileOpenRequested += path =>
{
    Console.WriteLine($"\nファイルが選択されました: {path}");
};

// 7. 拡張子を選択してファイル一覧を取得
navigation.Select(extensions.First().Identifier);
var files = navigation.GetCurrentSelectionView();
Console.WriteLine("\n=== ファイル一覧 ===");
foreach (var file in files)
{
    if (file is ItemFile f)
        Console.WriteLine($"  {f.FileName}");
}

// 8. 最初のファイルを選択（イベントが発火）
navigation.Select(files.First().Identifier);
```

### 例2: 作者でフィルタリング

```csharp
var navigation = app.ItemNavigationService;

// 作者一覧を取得
var authors = app.ItemGroupService.GetQueryFilters(QueryType.Author);

// 特定の作者を選択
var targetAuthor = authors.FirstOrDefault(a =>
    a is Author author && author.Name.Contains("ぷこるふ"));

if (targetAuthor != null)
{
    navigation.Select(targetAuthor.Identifier);
    
    // その作者のアイテムがカテゴリ別にフォルダ分けされて表示される
    var folders = navigation.GetCurrentSelectionView();
    foreach (var folder in folders)
    {
        if (folder is Folder f)
            Console.WriteLine($"{f.Title}: {f.ItemCount}件");
    }
}
```

## SearchFilesForCurrentItem()

現在のアイテム内でファイルを検索します。

```csharp
// アイテムを選択した状態で
var searchResults = navigation.SearchFilesForCurrentItem(".unitypackage");

foreach (var file in searchResults)
{
    if (file is ItemFile f)
        Console.WriteLine(f.FileName);
}
```

## Identifierのヘルパーメソッド

### GetPrefix()

プレフィックス付きのIdentifierを生成します。

```csharp
var identifier = ItemNavigationService.GetPrefix(
    ItemNavigationService.ItemPrefix,
    "e10abf3d-cca7-4117-a5e9-52926a4f8990"
);
// 結果: "item:e10abf3d-cca7-4117-a5e9-52926a4f8990"
```

### TryParseState()

Identifierからプレフィックスと値を抽出します。

```csharp
if (ItemNavigationService.TryParseState("item:xxxxx", out var key, out var value))
{
    Console.WriteLine($"Key: {key}, Value: {value}");
    // Key: item, Value: xxxxx
}
```

### ItemCategory.IsCategoryIdentifier()

Identifierがカテゴリ（type:またはcustom:）かどうかを判定します。

```csharp
// カテゴリIdentifierの判定
ItemCategory.IsCategoryIdentifier("type:1");         // true (Avatar)
ItemCategory.IsCategoryIdentifier("type:2");         // true (Clothing)
ItemCategory.IsCategoryIdentifier("custom:衣装");    // true
ItemCategory.IsCategoryIdentifier("item:xxxxx");     // false
ItemCategory.IsCategoryIdentifier("folder:xxxxx");   // false
```

### ItemCategory.FromIdentifier()

Identifierから`ItemCategory`を生成します。

```csharp
// type:IdentifierからItemTypeを取得
var category1 = ItemCategory.FromIdentifier("type:2");
Console.WriteLine(category1.Type);  // ItemType.Clothing

// custom:IdentifierからCustomCategoryを取得
var category2 = ItemCategory.FromIdentifier("custom:マイカテゴリ");
Console.WriteLine(category2.CustomCategory);  // "マイカテゴリ"

// ナビゲーションで取得したIdentifierをカテゴリに変換
var folder = navigation.GetCurrentSelectionView().OfType<Folder>().First();
if (ItemCategory.IsCategoryIdentifier(folder.Identifier))
{
    var category = ItemCategory.FromIdentifier(folder.Identifier);
    Console.WriteLine($"カテゴリ: {category.Type}");
}
```

#### 実践的な使用例

```csharp
// カテゴリフォルダを処理
var viewItems = navigation.GetCurrentSelectionView();
foreach (var item in viewItems)
{
    if (item is Folder folder && ItemCategory.IsCategoryIdentifier(folder.Identifier))
    {
        var category = ItemCategory.FromIdentifier(folder.Identifier);

        if (category.Type == ItemType.Clothing)
        {
            Console.WriteLine($"衣装カテゴリ: {folder.Title} ({folder.ItemCount}件)");
        }
        else if (category.Type == ItemType.Custom)
        {
            Console.WriteLine($"カスタムカテゴリ: {category.CustomCategory}");
        }
    }
}
```

## 次のステップ

[03 - アイテムの管理](./03-item-management.md) では、アイテムの作成・編集・削除について詳しく学びます。
