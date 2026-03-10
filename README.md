# VRC Avatar Explorer

![GitHub deployments](https://img.shields.io/github/deployments/puk06/VRC-Avatar-Explorer/release?style=flat)
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

## KonoAssetと違う点

| 項目 | Avatar Explorer | KonoAsset |
|------|----------------|-----------|
| エクスプローラー代替 | ファイル管理も兼用できる | アセット管理に特化 |
| 対応アバターの管理 | アバターアイテムで管理(事前登録が必要) | 文字列で管理 |
| Unitypackageインポート | パス自動変更・一括インポート対応 | 非対応 |
| アイテム追加時の設定 | 追加後にタグ等を設定（登録が速い） | 追加時に全て設定可能（登録が丁寧） |

## 導入方法

1. [最新のリリースページ](https://github.com/puk06/VRC-Avatar-Explorer/releases/latest)を開きます。
2. 使用しているOSに対応したファイルをダウンロードします。
3. 解凍したフォルダ内にある `AvatarExplorer` を実行してください。

---

##  コミット & ブランチ運用ポリシー (Commit & Branch Policy)

開発への貢献やメンテナンスに関しては、以下のルールを適用します。

### `main` ブランチ
- **言語**: 英語のみ (English only)
- **コミット形式**: [Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) の遵守。
- **マージ方法**: Squash merge のみ。

### その他のブランチ (`dev`, `feature/*` 等)
- **言語**: 制限なし（日本語・英語どちらも可）
- **コミット形式**: 自由。

## アーキテクチャと今後の方針

現在のUIレイヤーはMainWindowクラスにオーバーレイ処理などが集中しており、
設計上の課題として認識しています。MVVMへの移行は別ブランチにて改めて
取り組む予定のため、現時点では既存の設計に合わせた実装をお願いします。

### 命名規則
オーバーレイ専用のメソッドは `(オーバーレイ名)_(メソッド名)` のように、
オーバーレイ名をPrefixとして付けてください。

---
