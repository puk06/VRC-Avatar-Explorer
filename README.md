# VRC Avatar Explorer

English README: [README-en.md](README-en.md)

![GitHub Release](https://img.shields.io/github/v/release/puk06/VRC-Avatar-Explorer?label=Stable)
![GitHub Release](https://img.shields.io/github/v/release/puk06/VRC-Avatar-Explorer?include_prereleases&label=Pre-Release)

VRChatユーザー向けのシンプルかつ強力な**クロスプラットフォーム対応アセット管理ツール**です。

アセットファイルをBoothのアイテム情報と紐付けることで、エクスプローラー形式で自動的に整理・管理することができます。

---

## 出来ること

- **アセットファイルの自動整理**: アイテム情報に基づき、アセットをエクスプローラー形式にわかりやすく整理。
- **圧縮されたファイルの自動展開**: 複数の圧縮方式（zip、rar、7z、gz、tar）に対応したファイル自動展開機能。
- **アセット検索**: タイトルや作者、カテゴリなどの膨大なデータから必要なアセットを素早く特定。
- **対応アバター管理**: 各アセットがどのアバター向けかを管理。
- **共通素体グループ管理**: 共通素体のアバター同士をグループ化して効率化。
- **独自のUnitypackageインポート機能**: パスの自動変更や、複数ファイルの一括インポートに対応。
- **詳細なステータス管理**: アイテムタグ、メモ、実装・未実装アバターのステータス管理。
- **背景カスタマイズ**: アプリケーションの背景を好みの画像に設定可能。
- **ソフト内部から外部に直接D&D**: ソフトの画面上のファイルからUnityなどに直接D&Dできます。
- **未登録アバターの登録**: 未登録のアバターを一時的に追加し、対応アバターとして設定できます。後から正式なアイテムと連携できます。

## 免責事項

Boothからの自動アイテム取得機能に対応していますが、本アプリケーションはPixivプラットフォームを利用して開発されたものであり、Pixivが作成・配布しているアプリケーションではありません。

## KonoAssetと違う点

| 項目 | Avatar Explorer | KonoAsset |
|------|----------------|-----------|
| エクスプローラー代替 | ファイル管理も兼ねたエクスプローラー型 | アセット管理に特化 |
| 対応アバターの管理 | アバターアイテムで管理（仮アバター登録可） | 文字列ベースで管理 |
| Unitypackageインポート | パス自動変更・一括インポート対応 | 非対応 |

## 導入方法

1. [最新のリリースページ](https://github.com/puk06/VRC-Avatar-Explorer/releases/latest)を開きます。
2. 使用しているOSに対応したファイルをダウンロードします。
3. Windows以外の場合は、解凍したフォルダ内にある `AvatarExplorer` を実行してください。
4. Windowsの場合は、ダウンロードした `setup`（`.exe`）を実行してインストールしてください。
5. WindowsでSmartScreenが表示された場合は、`詳細情報` → `実行` の順に選択して続行してください。

リリースに関する注意事項は [RELEASE_NOTICES.md](RELEASE_NOTICES.md) を参照してください。
貢献や PR の運用ルールは [CONTRIBUTING.md](CONTRIBUTING.md) にまとめています。

---

## 開発環境

- 開発には **.NET 10.0 SDK** を使用します。
- 開発を始める前に、.NET 10.0 SDK をダウンロード・インストールしてください。

## プロジェクト構成

- **AvatarExplorer.Core**: AvatarExplorer のコア部分です。CLI などからこのライブラリを操作することで、新しい AvatarExplorer クライアントを作れます。シンプルなクラスライブラリで、UI に依存しません。
- **AvatarExplorer.UI**: AvatarExplorer.Core を UI で操作するためのアプリケーションです。Avalonia UI で作られています。
- **Tools/LocalizationKeyGenerator**: `AvatarExplorer.Core/Data/Localization/ja-JP.json` から `AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` を生成するツールです。`AvatarExplorer.Core` のビルド時に自動で生成されます。
---
