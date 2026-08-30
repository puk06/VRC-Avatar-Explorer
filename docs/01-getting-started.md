# 01 - 基本的な概念とセットアップ

この章では、AvatarExplorer.Coreの基本的な概念と、プロジェクトのセットアップ方法を学びます。

## プロジェクトの作成

まず、新しいコンソールアプリケーションプロジェクトを作成します。

```bash
dotnet new console -n MyAvatarExplorer
cd MyAvatarExplorer
```

## AvatarExplorer.Coreの参照

プロジェクトファイルにAvatarExplorer.Coreへの参照を追加します。

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\AvatarExplorer.Core\AvatarExplorer.Core.csproj" />
  </ItemGroup>

</Project>
```

> **注意**: AvatarExplorer.Coreは現在NuGetパッケージとして配布されていないため、プロジェクト参照を使用します。

## AvatarExplorerAppの基本

`AvatarExplorerApp`は、ライブラリのエントリーポイントとなるシングルトンクラスです。

```csharp
using AvatarExplorer.Core.Services.System;

// インスタンスの取得（シングルトンなので、どこからでも同じインスタンスが返る）
var app = AvatarExplorerApp.Instance;

// 初期化（必ず最初に呼び出す）
app.Initialize();
```

### Initialize()で行われる処理

`Initialize()`を呼び出すと、以下の処理が自動で行われます：

1. **設定の読み込み** - `RuntimeSettings`の読み込み
2. **データベースの読み込み** - 各リポジトリのデータ読み込み
   - `ItemRepository` - アイテム一覧
   - `CommonAvatarRepository` - 共通素体グループ
   - `TempAvatarRepository` - 仮アバター
   - `BulkImportPresetRepository` - 一括インポートプリセット
   - `VariationHashRepository` - バリエーションハッシュ
3. **検索インデックスの構築** - 高速検索のためのインデックス作成
4. **バックアップマネージャーの開始** - 自動バックアップの設定
5. **エラーハンドリングの設定** - エラーログの出力設定

> **重要**: `Initialize()`は一度だけ呼び出してください。複数回呼び出しても内部でガードされていますが、不要な処理は避けるべきです。

## 主要なサービスとリポジトリ

`AvatarExplorerApp`は以下のサービスとリポジトリを提供します：

### リポジトリ（データ管理）

| プロパティ | 型 | 説明 |
|---|---|---|
| `ItemRepository` | `ItemRepository` | アイテムのCRUD操作 |
| `CommonAvatarRepository` | `CommonAvatarRepository` | 共通素体グループの管理 |
| `TempAvatarRepository` | `TempAvatarRepository` | 仮アバターの管理 |
| `BulkImportPresetRepository` | `BulkImportPresetRepository` | 一括インポートプリセット |
| `VariationHashRepository` | `VariationHashRepository` | Boothバリエーションのハッシュ管理 |
| `RuntimeSettingsRepository` | `RuntimeSettingsRepository` | 設定の管理 |

### サービス（ビジネスロジック）

| プロパティ | 型 | 説明 |
|---|---|---|
| `ItemGroupService` | `ItemGroupService` | 横断的な操作（検索、削除、インポート/エクスポート） |
| `ItemNavigationService` | `ItemNavigationService` | ナビゲーション（選択状態の管理） |
| `BackupManager` | `BackupManager` | バックアップの管理 |
| `RuntimeSettings` | `RuntimeSettings` | 設定へのショートカット |

## IIdentifiableインターフェース

AvatarExplorer.Coreでは、ナビゲーション可能なオブジェクトはすべて`IIdentifiable`インターフェースを実装しています。

```csharp
namespace AvatarExplorer.Core.Interfaces;

public interface IIdentifiable
{
    string Identifier { get; }
}
```

この`Identifier`は、オブジェクトを一意に識別するための文字列です。

### Identifierの形式

Identifierは`プレフィックス:値`の形式を持っています：

| プレフィックス | 型 | 例 |
|---|---|---|
| `item:` | アイテム | `item:e10abf3d-cca7-4117-a5e9-52926a4f8990` |
| `avatar:` | アバター | `avatar:item:e10abf3d-...` |
| `author:` | 作者 | `author:ぷこるふの倉庫` |
| `folder:` | フォルダ | `folder:850F5169` |
| `file:` | ファイル | `file:a1b2c3d4...` |
| `commonavatar:` | 共通素体 | `commonavatar:xxxxxxxx-...` |
| `tempavatar:` | 仮アバター | `tempavatar:xxxxxxxx-...` |
| `type:` | タイプカテゴリ | `type:1` (Avatar) |
| `custom:` | カスタムカテゴリ | `custom: mycategory` |

## 基本的な使い方

### 全アイテムの一覧取得

```csharp
using AvatarExplorer.Core.Services.System;

var app = AvatarExplorerApp.Instance;
app.Initialize();

// ItemRepositoryから全アイテムを取得
var items = app.ItemRepository.GetAll();

foreach (var item in items)
{
    Console.WriteLine($"[{item.Category}] {item.Title} by {item.Author}");
    Console.WriteLine($"  ID: {item.Identifier}");
}
```

### 現在選択可能なアイテムの一覧取得

```csharp
var navigation = app.ItemNavigationService;
var viewItems = navigation.GetCurrentSelectionView();

foreach (var item in viewItems)
{
    Console.WriteLine(item.Identifier);
}
```

`GetCurrentSelectionView()`は、現在のナビゲーション状態に応じて返す内容が変わります：

- **初期状態**: 全アイテム（非表示を除く）
- **アバター選択後**: そのアバターが対応するアイテムをカテゴリ別にフォルダ分け
- **作者選択後**: その作者のアイテムをカテゴリ別にフォルダ分け
- **カテゴリ選択後**: そのカテゴリのアイテム一覧
- **アイテム選択後**: アイテム内のフォルダ一覧
- **フォルダ選択後**: ファイルの拡張子別フォルダ
- **拡張子選択後**: その拡張子のファイル一覧

## Itemモデル

`Item`クラスは、アイテムの情報を保持するモデルです。

```csharp
public class Item : IIdentifiable
{
    // 基本情報
    public string Title { get; }           // アイテム名
    public string Author { get; }          // 作者名
    public string AuthorId { get; }        // 作者ID（Boothのsubdomain）
    public int BoothId { get; }            // Booth商品ID

    // パス情報
    public string ItemPath { get; }        // アイテムのルートパス（相対パスの可能性）
    public ImmutableArray<string> ItemPaths { get; }  // 追加のフォルダパス

    // サムネイル
    public string ThumbnailFileName { get; }  // サムネイルファイル名

    // カテゴリ
    public ItemCategory Category { get; }     // カテゴリ（タイプ + カスタムカテゴリ）

    // 対応アバター
    public ImmutableArray<string> SupportedAvatars { get; }   // 対応アバターID一覧
    public ImmutableArray<string> ImplementedAvatars { get; } // 実装済みアバターID一覧

    // タグ・メモ
    public ImmutableArray<string> Tags { get; }  // タグ一覧
    public string ItemMemo { get; }              // メモ

    // 日時
    public string CreatedDate { get; }     // 作成日時（Unixタイムスタンプ）
    public string UpdatedDate { get; }     // 更新日時（Unixタイムスタンプ）

    // 状態
    public bool IsHidden { get; }          // 非表示フラグ
    public bool SkipIndirectCommonAvatarCheck { get; }  // 間接的な共通素体チェックから除外するフラグ

    // 識別子
    public string Identifier { get; }      // "item:" + Id
}
```

### ItemCategory

`ItemCategory`は、アイテムのカテゴリを表します。

```csharp
public record ItemCategory
{
    public ItemType Type { get; }          // 組み込みカテゴリタイプ
    public string CustomCategory { get; }  // カスタムカテゴリ名

    // 識別子
    public string Identifier { get; }      // "type:1" または "custom:カテゴリ名"

    // ローカライズ可能かどうか
    public bool IsLocalizable { get; }     // TypeがCustom/None以外の場合true
}
```

#### ItemCategoryのユーティリティメソッド

```csharp
// IdentifierがカテゴリIdentifierかどうかを判定
bool isCategory = ItemCategory.IsCategoryIdentifier("type:1");     // true
bool isCustom = ItemCategory.IsCategoryIdentifier("custom:衣装");  // true
bool isItem = ItemCategory.IsCategoryIdentifier("item:xxxxx");     // false

// IdentifierからItemCategoryを生成
var category1 = ItemCategory.FromIdentifier("type:2");             // ItemType.Clothing
var category2 = ItemCategory.FromIdentifier("custom:マイカテゴリ"); // CustomCategory = "マイカテゴリ"
var category3 = ItemCategory.FromIdentifier("invalid");            // ItemType.None

// 表示名を取得
var category = new ItemCategory(ItemType.Clothing);
Console.WriteLine(category.ToString());        // "ItemCategory.Clothing" (LocalizationKey)
Console.WriteLine(category.Identifier);        // "type:2"

var customCategory = new ItemCategory("オリジナル");
Console.WriteLine(customCategory.ToString());  // "オリジナル"
Console.WriteLine(customCategory.Identifier);  // "custom:オリジナル"
```

#### 実践的な使用例

```csharp
// ナビゲーションで選択されたIdentifierがカテゴリかどうかを判定
var identifier = "type:2";
if (ItemCategory.IsCategoryIdentifier(identifier))
{
    var category = ItemCategory.FromIdentifier(identifier);
    Console.WriteLine($"カテゴリ: {category}");
    Console.WriteLine($"タイプ: {category.Type}");
}

// カテゴリIdentifierからアイテムをフィルタリング
var categoryId = "type:2"; // Clothing
var targetCategory = ItemCategory.FromIdentifier(categoryId);
var clothingItems = app.ItemRepository.GetAll()
    .Where(i => i.Category.Equals(targetCategory));
```

### ItemType

`ItemType`は、アイテムのタイプを表す列挙型です。

```csharp
public enum ItemType
{
    None,        // 未設定
    Avatar,      // アバター
    Clothing,    // 衣装
    Texture,     // テクスチャ
    Gimmick,     // ギミック
    Accessory,   // アクセサリー
    HairStyle,   // 髪型
    Animation,   // アニメーション
    Tool,        // ツール
    Shader,      // シェーダー
    Custom,      // カスタムカテゴリ
    All,         // すべて（フィルタ用）
    Hidden       // 非表示（フィルタ用）
}
```

## 次のステップ

[02 - ナビゲーションシステム](./02-navigation-system.md) では、Identifierを使った選択操作と、ファイルオープンの仕組みについて詳しく学びます。
