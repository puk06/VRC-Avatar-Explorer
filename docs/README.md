# AvatarExplorer ドキュメント

AvatarExplorer の開発者向けドキュメントです。目的別に以下の2つのセクションに分かれています。

## クライアント開発（AvatarExplorer.Core）

`AvatarExplorer.Core` ライブラリを使って、独自のアバターエクスプローラークライアントアプリケーションを作成する方法を学ぶチュートリアルです。

- C#/.NET の開発経験がある方
- VRChat のアバター・アイテム管理ツールを作成したい方
- Booth で購入したアイテムを効率的に管理するアプリケーションを作りたい方

**[クライアント開発チュートリアルを読む](./client-development/README.md)**

| 章 | タイトル |
|---|---|
| [01](./client-development/01-getting-started.md) | [基本的な概念とセットアップ](./client-development/01-getting-started.md) |
| [02](./client-development/02-navigation-system.md) | [ナビゲーションシステム](./client-development/02-navigation-system.md) |
| [03](./client-development/03-item-management.md) | [アイテムの管理](./client-development/03-item-management.md) |
| [04](./client-development/04-avatar-management.md) | [共通素体・仮アバターの管理](./client-development/04-avatar-management.md) |
| [05](./client-development/05-booth-integration.md) | [Booth連携](./client-development/05-booth-integration.md) |
| [06](./client-development/06-unitypackage-processing.md) | [Unitypackageの処理](./client-development/06-unitypackage-processing.md) |
| [07](./client-development/07-runtime-settings.md) | [設定の管理](./client-development/07-runtime-settings.md) |
| [08](./client-development/08-import-export.md) | [データのインポート/エクスポート](./client-development/08-import-export.md) |
| [09](./client-development/09-search.md) | [検索機能](./client-development/09-search.md) |
| [10](./client-development/10-complete-example.md) | [完全な実装例](./client-development/10-complete-example.md) |
| [11](./client-development/11-json-utilities.md) | [JSONユーティリティ](./client-development/11-json-utilities.md) |

## UI開発（Avalonia Style / Template）

`AvatarExplorer.UI` のXAMLを読み書きするときに必要な、Avaloniaの **Style** と **Template** をこのリポジトリの実コードを例に解説したガイドです。

- C# は分かるが、Avalonia の XAML は雰囲気で読んでいる方
- Style / ControlTemplate / DataTemplate の違いを整理したい方
- このプロジェクトのUIルールを確認したい方

**[Avalonia Style / Template 入門ガイドを読む](./ui-development/avalonia-style-guide.md)**

## ライセンス

このプロジェクトは **GNU Affero General Public License v3.0 (AGPL-3.0)** のもとでライセンスされています。詳細は [LICENSE](../LICENSE) を参照してください。
