# CONTRIBUTING

## 日本語

このリポジトリへの貢献やメンテナンスに関するルールをまとめています。

### バージョンタグ運用

本プロジェクトのタグ形式は `v<version>` をベースにし、接尾辞は以下のみ使用します。

- `-beta.X`（プレリリース）
- `-stable`（安定版タグ）

例:

- `v2.0.0-beta.1`
- `v2.0.0-stable`

上記以外の接尾辞は使用しません。

#### セマンティック バージョニング基準

本プロジェクトでは、以下の基準でバージョンを決定します。

| バージョン | 基準 | 例 |
|-----------|------|-----|
| **v2.0.0** | V1 からの完全な書き直し（固定）。本バージョンは変わりません。 | v2.0.0, v2.0.0-beta.1 |
| **v0.x.0** （MINOR） | ユーザーが体験して変わったことがわかる機能追加・仕様変更など。 | 新機能追加、既存機能の大幅改善 |
| **v0.0.x** （PATCH） | コードのみの変更で、ユーザー体験に直結しない改善。バグ修正、内部リファクタリング、パフォーマンス改善など。 | `FileNameUtils` の安全化修正、セキュリティ fix |

### ブランチとコミット

#### `main` ブランチ
- 言語: 英語のみ
- コミット形式: [Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) を使用してください。
- マージ方法: Squash merge のみ

#### その他のブランチ (`dev`, `feature/*` など)
- 言語: 制限なし（日本語・英語どちらも可）
- コミット形式: 自由

#### バージョン更新時の運用
- ブランチ名: `chore/bump-version-<version>`
- コミットメッセージ: `chore: bump version to <version>`

例:
- `chore/bump-version-0.3.2`
- `chore: bump version to 0.3.2`

### Pull Request

PR タイトルは、Squash merge 時のコミットメッセージになるため、[Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) 形式で記載してください。

```text
<type>[(<scope>)]: <subject>
```

- type: コミットの種類
- scope: 変更の対象範囲（任意）
- subject: 変更内容の短い説明

使用できる type:
- `feat`: 新機能追加
- `fix`: バグ修正
- `docs`: ドキュメント更新
- `ci`: CI/CD パイプライン・GitHub Actions 関連
- `refactor`: コードの構造改善（機能変更なし）
- `perf`: パフォーマンス改善
- `test`: テスト追加・修正
- `chore`: ビルド設定やツールの更新など

PR 説明文には、目的や背景、関連 Issue、実装上の注意点を簡潔に記載してください。

PR 前の確認事項:
- `AvatarExplorer.UI` または `AvatarExplorer.Core` をビルドして、エラーが出ないことを確認する
- `Tools/LocalizationKeyGenerator` を実行して、`AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` を再生成する（変更がない場合でも実行する）

### AI 利用時のガイドライン

GitHub Copilot などの AI ツールを使うことは問題ありませんが、生成結果は必ず人の目でレビュー・検証してください。

確認ポイント:
- ロジックが正しいか
- エッジケースを考慮しているか
- プロジェクトのコーディング規約に準拠しているか
- パフォーマンス上の問題がないか

### アーキテクチャメモ

現在の UI レイヤーでは、オーバーレイ関連の処理が MainWindow クラスに集中しています。MVVM 移行は別ブランチで進める予定のため、現時点では既存の設計に合わせて実装してください。

#### 命名規則（暫定）
- オーバーレイ専用のメンバーは `<Overlay名>_<メンバー名>` の形式で命名する
- private フィールドは `_` + camelCase を使用する

例:
- `_hogeOverlay_foo`
- `HogeOverlay_DoSomething`

---

## English

This repository contribution and maintenance guide summarizes the rules used by the project.

### Version Tag Policy

This project uses `v<version>` as the base tag format, and only the following suffixes are allowed.

- `-beta.X` (pre-release)
- `-stable` (stable release tag)

Examples:

- `v2.0.0-beta.1`
- `v2.0.0-stable`

No other suffixes are used.

#### Semantic Versioning Guidelines

This project determines versions based on the following criteria.

| Version | Criteria | Examples |
|---------|----------|----------|
| **v2.0.0** | Complete rewrite from V1 (fixed). This version remains unchanged. | v2.0.0, v2.0.0-beta.1 |
| **v0.x.0** (MINOR) | Feature additions or specification changes that users notice. | New features, significant improvements to existing features |
| **v0.0.x** (PATCH) | Code-only changes with no direct user impact. Bug fixes, internal refactoring, performance improvements, etc. | FileNameUtils security hardening, security fixes |

### Branches and commits

#### `main` branch
- Language: English only
- Commit format: Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
- Merge method: Squash merge only

#### Other branches (`dev`, `feature/*`, etc.)
- Language: No restriction (Japanese or English)
- Commit format: Free style

#### Version bump workflow
- Branch name: `chore/bump-version-<version>`
- Commit message: `chore: bump version to <version>`

Examples:
- `chore/bump-version-0.3.2`
- `chore: bump version to 0.3.2`

### Pull Requests

Because the PR title becomes the commit message on squash merge, write it in [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) format.

```text
<type>[(<scope>)]: <subject>
```

- type: Commit type
- scope: Affected area (optional)
- subject: Short description of the change

Available types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation update
- `ci`: CI/CD pipeline or GitHub Actions changes
- `refactor`: Code structure improvement (no behavior change)
- `perf`: Performance improvement
- `test`: Test additions or updates
- `chore`: Build configuration or tooling updates

Write the PR description briefly with the purpose, background, related issues, and any important implementation notes.

Pre-PR checklist:
- Build `AvatarExplorer.UI` or `AvatarExplorer.Core` and confirm there are no errors
- Run `Tools/LocalizationKeyGenerator` to regenerate `AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` (run even if there are no changes)

### AI usage guidelines

Using AI tools such as GitHub Copilot is allowed, but generated results must always be reviewed and validated by a human.

Review points:
- Is the logic correct?
- Are edge cases considered?
- Does it follow the project's coding conventions?
- Are there performance concerns?

### Architecture note

In the current UI layer, overlay-related processing is concentrated in the MainWindow class. MVVM migration will be handled in a separate branch, so implement changes in a way that follows the current design.

#### Temporary naming rules
- Members dedicated to an overlay should follow `<OverlayName>_<MemberName>`
- Private fields should use `_` + camelCase

Examples:
- `_hogeOverlay_foo`
- `HogeOverlay_DoSomething`
