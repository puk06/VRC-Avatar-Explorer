# VRC Avatar Explorer

English README: [README-en.md](README-en.md)

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
- **未登録アバターの登録**: 未登録のアバターを一時的に追加し、対応アバターとして設定できます。後から正式なアイテムと連携できます。

## KonoAssetと違う点

| 項目 | Avatar Explorer | KonoAsset |
|------|----------------|-----------|
| エクスプローラー代替 | ファイル管理も兼ねたエクスプローラー型 | アセット管理に特化 |
| 対応アバターの管理 | アバターアイテムで管理（仮アバター登録可） | 文字列ベースで管理 |
| Unitypackageインポート | パス自動変更・一括インポート対応 | 非対応 |
| アイテム追加時の設定 | 追加後にタグ等を設定（スピード重視） | 追加時に全て設定（正確性重視） |

## 導入方法

1. [最新のリリースページ](https://github.com/puk06/VRC-Avatar-Explorer/releases/latest)を開きます。
2. 使用しているOSに対応したファイルをダウンロードします。
3. 解凍したフォルダ内にある `AvatarExplorer` を実行してください。

リリースに関する注意事項は [RELEASE_NOTICES.md](RELEASE_NOTICES.md) を参照してください。

---

## 開発環境

- 開発には **.NET 10.0 SDK** を使用します。
- 開発を始める前に、.NET 10.0 SDK をダウンロード・インストールしてください。

## プロジェクト構成

- **AvatarExplorer.Core**: AvatarExplorer のコア部分です。CLI などからこのライブラリを操作することで、新しい AvatarExplorer クライアントを作れます。シンプルなクラスライブラリで、UI に依存しません。
- **AvatarExplorer.UI**: AvatarExplorer.Core を UI で操作するためのアプリケーションです。Avalonia UI で作られています。
- **Tools/LocalizationKeyGenerator**: `AvatarExplorer.Core/Data/Localization/ja-JP.json` から `AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` を生成するツールです。`AvatarExplorer.Core` のビルド時に自動で生成されます。

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

### プルリクエスト（Pull Request）

**タイトル形式（必須）**

Squash merge 時のコミットメッセージになるため、[Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) 形式で記述してください。

```
<type>[(<scope>)]: <subject>
```

- **type**: コミットの種類（必須）
- **scope**: 変更の対象範囲（任意）- 影響を受けるモジュールやコンポーネント名
- **subject**: 変更内容の短い説明（必須）

**type の種類**:
- `feat`: 新機能追加
- `fix`: バグ修正
- `docs`: ドキュメント更新
- `ci`: CI/CD パイプライン・GitHub Actions 関連
- `refactor`: コードの構造改善（機能変更なし）
- `perf`: パフォーマンス改善
- `test`: テスト追加・修正
- `chore`: ビルド設定やツールの更新など

**タイトル例**:
- `feat(avatar): add support for custom avatar names` (scope あり)
- `feat: improve search performance` (scope なし)
- `fix(ui): resolve overlay display bug on startup`
- `docs: update contribution guidelines`
- `ci: add automated release workflow`

**説明文（Description）**:
- PR の目的や背景を簡潔に記述（日本語でも可）
- 関連する Issue がある場合は `Closes #123` のように参照
- 主な変更点や実装上の注意点があれば記載

**PR 前の確認事項（必須）**:
- ✅ **ビルドが成功すること**: `AvatarExplorer.UI` または `AvatarExplorer.Core` をビルドして、エラーが出ないことを確認してください
- ✅ **Localization Key の再生成**: `Tools/LocalizationKeyGenerator` を実行して、`AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` を自動生成してください（変更がない場合でも実行してください）

これらのチェックは CI 上でも実行されますが、事前に実施することで、PR マージ前の手戻りを防ぐことができます。

**ブランチ内の個別コミット**:
- feature ブランチ内の個別コミットメッセージは自由です
- main へのマージ時に Squash merge されるため、履歴を気にせず開発できます

## AI 利用時のガイドライン

このプロジェクトへの貢献時に AI ツール（GitHub Copilot など）を利用することは問題ありません。以下の点にご注意ください。

**最重要事項**:
- 🔍 **すべてのコード変更は、あなたの目で必ずレビュー・検証してください**。AI が生成したコードであっても、バグの混入を防ぎ、プロジェクトの品質を維持するためです。

**利用例**:
- ✅ 関数やメソッドの一部を補完させる
- ✅ ロジックのスニペット生成
- ✅ GitHub Actions などの CI/CD 設定
- ✅ ドキュメント作成
- ✅ コードのリファクタリング提案

**レビュー時の確認ポイント**:
- ロジックが正しいか
- エッジケースを考慮しているか
- プロジェクトのコーディング規約に準拠しているか
- パフォーマンスに問題がないか

## アーキテクチャと今後の方針

現在のUIレイヤーはMainWindowクラスにオーバーレイ処理などが集中しており、
設計上の課題として認識しています。MVVMへの移行は別ブランチにて改めて
取り組む予定のため、現時点では既存の設計に合わせた実装をお願いします。

### 命名規則（暫定）

現在、MainWindow にロジックが集約されているため、
オーバーレイごとの責務を明確にする目的で以下の命名規則を使用します。

将来的に MVVM へ移行する際に、この規則は廃止される予定です。

- オーバーレイ専用のメンバーは `<Overlay名>_<メンバー名>` の形式で命名する
- private フィールドは `_` + camelCase を使用する

例:
- `_hogeOverlay_foo`
- `HogeOverlay_DoSomething`
---
