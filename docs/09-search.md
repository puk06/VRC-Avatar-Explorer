# 09 - 検索機能

この章では、`ItemGroupService`を使った検索機能について学びます。

## SearchItems() - 統合検索

```csharp
using AvatarExplorer.Core.Models.Search;

var app = AvatarExplorerApp.Instance;
app.Initialize();

// 検索を実行
var results = app.ItemGroupService.SearchItems(
    searchString: "アバター名",
    types: SearchResultTypes.Items
);

Console.WriteLine($"検索結果: {results.Length}件");

foreach (var identifier in results)
{
    Console.WriteLine(identifier);
}
```

### locKeyProviderについて

`locKeyProvider`は、**カテゴリ検索時にのみ使用される**関数で、表示名をLocalizationKeyに変換するためのものです。

#### カテゴリの保存形式

カテゴリは`item.Category.ToString()`の結果で検索インデックスに保存されています：

| カテゴリタイプ | 保存される値 | 例 |
|---|---|---|
| `ItemType`（通常のカテゴリ） | `ItemType.GetLocalizationKey()` | `"ItemCategory.Clothing"` |
| `CustomCategory`（カスタムカテゴリ） | `CustomCategory`の文字列そのまま | `"マイカテゴリ"` |

つまり：
- **ItemTypeのカテゴリ**（Clothing、Avatarなど）は、**LocalizationKey**で検索される
- **CustomCategory**は、**そのままの文字列**で検索される

#### locKeyProviderの役割

ユーザーが「衣装」と入力した場合、これをLocalizationKey（`"ItemCategory.Clothing"`）に変換する必要があります。`locKeyProvider`はこの変換を行います。

```csharp
// 例：日本語のカテゴリ名をLocalizationKeyに変換する関数
string? ConvertToLocKey(string displayName)
{
    return displayName switch
    {
        "衣装" => ItemType.Clothing.GetLocalizationKey(),
        "アバター" => ItemType.Avatar.GetLocalizationKey(),
        "テクスチャ" => ItemType.Texture.GetLocalizationKey(),
        "アクセ" => ItemType.Accessory.GetLocalizationKey(),
        _ => displayName  // 変換できない場合はそのまま返す
    };
}

// 使用例
var results = app.ItemGroupService.SearchItems(
    searchString: "category=\"衣装\"",
    types: SearchResultTypes.Items,
    locKeyProvider: ConvertToLocKey
);
```

#### カスタムカテゴリの場合

カスタムカテゴリはそのままの文字列で保存されているため、`locKeyProvider`は不要です：

```csharp
// カスタムカテゴリ「マイカテゴリ」で検索（locKeyProvider不要）
var results = app.ItemGroupService.SearchItems(
    "category=\"マイカテゴリ\"",
    SearchResultTypes.Items
);
```

> **注意**: `locKeyProvider`を指定しない場合、カテゴリ検索は入力された文字列をそのまま使用します。ItemTypeのカテゴリで検索する場合は、LocalizationKeyを直接指定するか、`locKeyProvider`を設定してください。

## 検索クエリの構文

### 基本的な検索

スペース区切りでAND検索：

```csharp
// 「衣装」かつ「青」を含むアイテム
var results = app.ItemGroupService.SearchItems("衣装 青", SearchResultTypes.Items);
```

### OR検索

`OR=true`を使用：

```csharp
// 「衣装」または「アクセ」を含むアイテム
var results = app.ItemGroupService.SearchItems("衣装 アクセ OR=true", SearchResultTypes.Items);
```

### 除外検索

`~`で始まるトークン：

```csharp
// 「衣装」を含むが「赤」を含まないアイテム
var results = app.ItemGroupService.SearchItems("衣装 ~赤", SearchResultTypes.Items);
```

### フィールド指定検索

`フィールド名="値"`の形式：

```csharp
// 作者名が「ぷこるふ」のアイテム
var results = app.ItemGroupService.SearchItems("author=\"ぷこるふ\"", SearchResultTypes.Items);

// タイトルに「フォト」を含むアイテム
var results2 = app.ItemGroupService.SearchItems("title=\"フォト\"", SearchResultTypes.Items);

// カテゴリで検索
// カテゴリは、ItemTypeの場合はLocalizationKey、CustomCategoryの場合はそのままの文字列で検索されます。
// 現在の言語で検索したい場合は、locKeyProviderでLocalizationKeyに変換する関数を渡してください。
// LocalizationKeyは、ItemType.GetLocalizationKey()で取得できます。

// 例1: CustomCategoryで検索（locKeyProvider不要）
var results3 = app.ItemGroupService.SearchItems("category=\"マイカテゴリ\"", SearchResultTypes.Items);

// 例2: ItemTypeで検索（locKeyProviderを使用）
// 「衣装」をLocalizationKeyに変換する関数をlocKeyProviderに渡す
var results4 = app.ItemGroupService.SearchItems(
    "category=\"衣装\"",
    SearchResultTypes.Items,
    locKeyProvider: name => name switch
    {
        "衣装" => ItemType.Clothing.GetLocalizationKey(),
        "アバター" => ItemType.Avatar.GetLocalizationKey(),
        _ => name
    }
);

// 例3: ItemTypeで検索（LocalizationKeyを直接指定）
var results5 = app.ItemGroupService.SearchItems($"category=\"{ItemType.Clothing.GetLocalizationKey()}\"", SearchResultTypes.Items);

// 例4: ItemCategory.FromIdentifierを使ってカテゴリIdentifierから検索
// ナビゲーションで取得したカテゴリIdentifierをそのまま使える
var categoryId = "type:2"; // ItemNavigationServiceから取得したIdentifier
var category = ItemCategory.FromIdentifier(categoryId);
var results6 = app.ItemGroupService.SearchItems(
    $"category=\"{category.ToString()}\"",
    SearchResultTypes.Items
);
```

#### ItemCategory.FromIdentifierを使った応用例

```csharp
// ナビゲーションで選択されたカテゴリフォルダから検索クエリを生成
var navigation = app.ItemNavigationService;
var viewItems = navigation.GetCurrentSelectionView();

foreach (var item in viewItems)
{
    if (item is Folder folder && ItemCategory.IsCategoryIdentifier(folder.Identifier))
    {
        var category = ItemCategory.FromIdentifier(folder.Identifier);

        // カテゴリで検索するクエリを生成
        string searchQuery;
        if (category.Type == ItemType.Custom)
        {
            // CustomCategoryはそのままの文字列で検索
            searchQuery = $"category=\"{category.CustomCategory}\"";
        }
        else
        {
            // ItemTypeはToString()（LocalizationKey）で検索
            searchQuery = $"category=\"{category.ToString()}\"";
        }

        Console.WriteLine($"検索クエリ: {searchQuery}");
        var results = app.ItemGroupService.SearchItems(searchQuery, SearchResultTypes.Items);
        Console.WriteLine($"  {folder.Title}: {results.Length}件");
    }
}
```

### 利用可能なフィールド

| フィールド名 | 説明 |
|---|---|
| `title` | アイテム名 |
| `author` | 作者名 |
| `category` | カテゴリ名（ItemTypeはLocalizationKey、CustomCategoryはそのままの文字列） |
| `tag` | タグ |
| `memo` | メモ |
| `supported` | 対応アバター名 |
| `implemented` | 実装済みアバター名 |
| `notimplemented` | 未実装アバター名 |
| `commonavatar` | 共通素体グループ名 |
| `boothid` / `booth` | 仮アバターのBooth商品ID |

## SearchResultTypes

```csharp
[Flags]
public enum SearchResultTypes
{
    None = 0,
    Items = 1,           // アイテム
    CommonAvatar = 2,    // 共通素体
    TempAvatar = 4,      // 仮アバター
    All = Items | CommonAvatar | TempAvatar
}
```

### 複数のタイプを検索

```csharp
// アイテムと共通素体を検索
var results = app.ItemGroupService.SearchItems(
    "検索文字列",
    SearchResultTypes.Items | SearchResultTypes.CommonAvatar
);

// 全てを検索
var allResults = app.ItemGroupService.SearchItems(
    "検索文字列",
    SearchResultTypes.All
);
```

## 検索インデックス

検索は、事前に構築されたインデックスを使用することで高速化されています。

### RebuildIndices() - インデックスの再構築

```csharp
// 通常はInitialize()時に自動で構築される
// データベースを直接操作した場合は手動で再構築
app.ItemGroupService.RebuildIndices();
```

> **注意**: `ItemRepository`や`CommonAvatarRepository`の操作後は自動的にインデックスが再構築されます。

## 実践的な例

### 例1: 基本的な検索UI

```csharp
Console.Write("検索: ");
var query = Console.ReadLine();

if (string.IsNullOrWhiteSpace(query)) return;

var results = app.ItemGroupService.SearchItems(query, SearchResultTypes.Items);

Console.WriteLine($"\n検索結果: {results.Length}件\n");

foreach (var id in results.Take(20))
{
    var item = app.ItemRepository.Get(id);
    if (item != null)
    {
        Console.WriteLine($"  [{item.Category}] {item.Title} by {item.Author}");
    }
}

if (results.Length > 20)
{
    Console.WriteLine($"  ... 他 {results.Length - 20}件");
}
```

### 例2: フィルタリングとの組み合わせ

```csharp
// カテゴリでフィルタしつつ検索
var categoryItems = app.ItemRepository.GetAll()
    .Where(i => i.Category.Type == ItemType.Clothing);

// 検索結果とフィルタの交差
var searchResults = app.ItemGroupService.SearchItems("青", SearchResultTypes.Items);
var filtered = categoryItems
    .Where(i => searchResults.Contains(i.Identifier))
    .ToList();

Console.WriteLine($"衣装カテゴリで「青」を含むアイテム: {filtered.Count}件");
```

### 例3: 作者での検索

```csharp
// 作者名で検索
var authorResults = app.ItemGroupService.SearchItems(
    "author=\"ぷこるふの倉庫\"",
    SearchResultTypes.Items
);

Console.WriteLine($"ぷこるふの倉庫のアイテム: {authorResults.Length}件");
```

### 例4: タグでの検索

```csharp
// 特定のタグを含むアイテム
var tagResults = app.ItemGroupService.SearchItems(
    "tag=\"無料\"",
    SearchResultTypes.Items
);

Console.WriteLine($"「無料」タグ付きアイテム: {tagResults.Length}件");
```

### 例5: 対応アバターでの検索

```csharp
// 特定のアバターに対応するアイテム
var avatarResults = app.ItemGroupService.SearchItems(
    "supported=\"せかい\"",
    SearchResultTypes.Items
);

Console.WriteLine($"「せかい」対応アイテム: {avatarResults.Length}件");
```

### 例6: 複雑な検索

```csharp
// 衣装カテゴリで「セーター」または「ニット」を含むが、「無料」は除く
// カテゴリはLocalizationKeyで指定（またはlocKeyProviderを使用）
var clothingKey = ItemType.Clothing.GetLocalizationKey();
var results = app.ItemGroupService.SearchItems(
    $"category=\"{clothingKey}\" (セーター | ニット) ~無料",
    SearchResultTypes.Items
);

foreach (var id in results)
{
    var item = app.ItemRepository.Get(id);
    if (item != null)
    {
        Console.WriteLine($"{item.Title} by {item.Author}");
    }
}
```

### 例6b: locKeyProviderを使った複雑な検索

```csharp
// locKeyProviderを使って、現在の言語でカテゴリを指定
var results = app.ItemGroupService.SearchItems(
    "category=\"衣装\" (セーター | ニット) ~無料",
    SearchResultTypes.Items,
    locKeyProvider: name => name switch
    {
        "衣装" => ItemType.Clothing.GetLocalizationKey(),
        _ => name
    }
);
```

### 例7: 共通素体の検索

```csharp
// 共通素体を検索
var commonResults = app.ItemGroupService.SearchItems(
    "グループ名",
    SearchResultTypes.CommonAvatar
);

foreach (var id in commonResults)
{
    var common = app.CommonAvatarRepository.Get(id);
    if (common != null)
    {
        Console.WriteLine($"共通素体: {common.GroupName}");
        Console.WriteLine($"  アバター数: {common.Avatars.Count}");
    }
}
```

### 例8: リアルタイム検索

```csharp
// リアルタイム検索の例
async Task RealtimeSearch(string query)
{
    // 検索インデックスが構築されていることを確認
    var results = app.ItemGroupService.SearchItems(query, SearchResultTypes.Items);

    // 結果を表示
    foreach (var id in results.Take(10))
    {
        var item = app.ItemRepository.Get(id);
        if (item != null)
        {
            Console.WriteLine($"  {item.Title}");
        }
    }
}

// 入力ごとに検索
while (true)
{
    Console.Write("\n検索 (Enterで終了): ");
    var input = Console.ReadLine();

    if (string.IsNullOrEmpty(input)) break;

    await RealtimeSearch(input);
}
```

## SearchQueryParser

検索文字列をパースするためのユーティリティ：

```csharp
using AvatarExplorer.Core.Models.Search;

var query = SearchQueryParser.Parse("衣装 ~赤 author=\"VRChat\"");

Console.WriteLine($"IsOr: {query.IsOr}");
Console.WriteLine($"IncludeHidden: {query.IncludeHidden}");
Console.WriteLine($"Tokens: {query.Tokens.Count}");

foreach (var token in query.Tokens)
{
    Console.WriteLine($"  Field: {token.Field ?? "(none)"}");
    Console.WriteLine($"  Value: {token.Value}");
    Console.WriteLine($"  IsNegation: {token.IsNegation}");
}
```

## SearchFilesForCurrentItem()

アイテム内のファイルを検索します。フォルダが選択されている場合はそのフォルダ内で、選択されていない場合はアイテム全体から検索します。

```csharp
var navigation = app.ItemNavigationService;

// アイテムを選択した状態で
navigation.Select("item:xxxxx");

// ファイルを検索
var fileResults = navigation.SearchFilesForCurrentItem(".unitypackage");

Console.WriteLine($"Unitypackageファイル: {fileResults.Length}件");

foreach (var file in fileResults)
{
    if (file is ItemFile f)
    {
        Console.WriteLine($"  {f.FileName}");
    }
}
```

## ItemRepository.SearchItemFiles()

アイテム内のファイルを直接検索：

```csharp
var files = app.ItemRepository.SearchItemFiles("item:xxxxx", "prefab");

foreach (var file in files)
{
    Console.WriteLine($"{file.FileName} ({file.Extension})");
}
```

## 次のステップ

[10 - 完全な実装例](./10-complete-example.md) では、コンソールアプリケーションの完全な実装例を紹介します。
