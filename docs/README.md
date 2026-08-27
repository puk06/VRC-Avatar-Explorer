# AvatarExplorer.Core クライアント開発チュートリアル

このチュートリアルでは、`AvatarExplorer.Core`ライブラリを使用して、独自のアバターエクスプローラークライアントアプリケーションを作成する方法を学びます。

## 対象読者

- C#/.NETの開発経験がある方
- VRChatのアバター・アイテム管理ツールを作成したい方
- Boothで購入したアイテムを効率的に管理するアプリケーションを作りたい方

## 目次

| 章 | タイトル | 説明 |
|---|---|---|
| [01](./01-getting-started.md) | [基本的な概念とセットアップ](./01-getting-started.md) | プロジェクトの作成、初期化、基本的な概念 |
| [02](./02-navigation-system.md) | [ナビゲーションシステム](./02-navigation-system.md) | Identifier、選択操作、ファイルオープン |
| [03](./03-item-management.md) | [アイテムの管理](./03-item-management.md) | アイテムの作成・編集・削除 |
| [04](./04-avatar-management.md) | [共通素体・仮アバターの管理](./04-avatar-management.md) | 共通素体グループ、仮アバター |
| [05](./05-booth-integration.md) | [Booth連携](./05-booth-integration.md) | Booth APIを使ったアイテム情報取得 |
| [06](./06-unitypackage-processing.md) | [Unitypackageの処理](./06-unitypackage-processing.md) | パス変更、マージ機能 |
| [07](./07-runtime-settings.md) | [設定の管理](./07-runtime-settings.md) | RuntimeSettingsの読み書き |
| [08](./08-import-export.md) | [データのインポート/エクスポート](./08-import-export.md) | CSV、KonoAsset形式など |
| [09](./09-search.md) | [検索機能](./09-search.md) | 検索クエリ、フィルタリング |
| [10](./10-complete-example.md) | [完全な実装例](./10-complete-example.md) | コンソールアプリの完全な例 |

## 基本的な概念

AvatarExplorer.Coreの基本的な思想は**ナビゲーション**です。

ユーザーが以下のように段階的に選択していくことで、目的のファイルに辿り着く設計になっています：

```
アバター選択 → カテゴリ選択 → アイテム選択 → フォルダ選択 → 拡張子選択 → ファイル選択
```

### 主要なコンポーネント

```
AvatarExplorerApp (シングルトン)
├── ItemRepository              # アイテムの管理
├── CommonAvatarRepository      # 共通素体グループの管理
├── TempAvatarRepository        # 仮アバターの管理
├── BulkImportPresetRepository  # 一括インポートプリセット
├── VariationHashRepository     # バリエーションハッシュ
├── ItemGroupService            # 横断的な操作（検索、削除など）
├── ItemNavigationService       # ナビゲーション管理
├── RuntimeSettingsRepository   # 設定管理
└── BackupManager               # バックアップ管理
```

## クイックスタート

```csharp
using AvatarExplorer.Core.Services.System;

// 1. インスタンス取得と初期化
var app = AvatarExplorerApp.Instance;
app.Initialize();

// 2. ナビゲーションサービスの取得
var navigation = app.ItemNavigationService;

// 3. 現在の選択可能なアイテム一覧を取得
var viewItems = navigation.GetCurrentSelectionView();

// 4. アイテムを選択
navigation.Select("item:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");

// 5. ファイル選択イベントの購読
navigation.FileOpenRequested += path =>
{
    Console.WriteLine($"ファイルが選択されました: {path}");
};
```

## 必要な環境

- .NET 10.0 以降（標準で.NET 10を採用）
- C# 14 以降

## ライセンス

このプロジェクトは **GNU Affero General Public License v3.0 (AGPL-3.0)** のもとでライセンスされています。

### AGPL-3.0について

AGPL-3.0は、GPLにネットワークサービス条項を追加したコピーレフトなライセンスです。

**主な特徴:**
- ソースコードの開示が必要
- 改変版も同じライセンスで提供する必要あり
- **ネットワーク経由で利用される場合もソースコード開示義務が生じる**（GPLとの違い）

### クライアント開発における注意

AvatarExplorer.Coreを使用してクライアントアプリケーションを作成する場合、そのアプリケーションもAGPL-3.0の条件に従う必要があります。

詳細については、[LICENSE](../LICENSE)ファイルおよび[GNU公式ウェブサイト](https://www.gnu.org/licenses/agpl-3.0.html)をご確認ください。
