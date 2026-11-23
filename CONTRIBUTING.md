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
- subject: 変更内容の短い説明（小文字で始める）

使用できる type:
- `feat`: 新機能追加
- `fix`: バグ修正
- `docs`: ドキュメント更新
- `ci`: CI/CD パイプライン・GitHub Actions 関連
- `refactor`: コードの構造改善（機能変更なし）
- `perf`: パフォーマンス改善
- `test`: テスト追加・修正
- `chore`: ビルド設定やツールの更新など

#### Subject の書き方

subject は以下のルールに従ってください：

- **小文字で始める**（例：`add`, `fix`, `update` など）
- 現在形を使う（過去形 `fixed` ではなく `fix`）
- 末尾にピリオド（`.`）を付けない
- 命令形で記述（「〜する」という意味で）

**良い例**：
- `feat(avatars): add avatar filtering by hair color`
- `fix(database): fix null reference in avatar cache`
- `docs(readme): update installation instructions`
- `refactor(ui): simplify overlay initialization logic`

**悪い例**：
- ❌ `Add avatar filtering by hair color` (大文字で始まっている)
- ❌ `Fixed null reference exception` (過去形)
- ❌ `Update installation instructions.` (ピリオド付き)

PR 説明文には、目的や背景、関連 Issue、実装上の注意点を簡潔に記載してください。

PR 前の確認事項:
- `AvatarExplorer.UI` または `AvatarExplorer.Core` をビルドして、エラーが出ないことを確認する
- `Tools/LocalizationKeyGenerator` を実行して、`AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` を再生成する（変更がない場合でも実行する）
- 新規に初期化処理を追加した場合、`IInitializable` / `InitializableRegistry` の規約に従っていることを確認する

### UI コードの規約

#### 初期化（IInitializable / InitializableRegistry）

`AvatarExplorer.UI` では、ViewModel や View のコンストラクタで、起動順に依存する他のインスタンスを参照しないでください。
インスタンス間の構築順は保証されておらず、`InstanceRepository.MainWindow` や `MainWindowViewModel.Instance` などの
シングルトンがまだ `null` である可能性があります。また、`this.WhenAnyValue(...)` をコンストラクタで購読すると、
オブジェクトが完全に構築される前に通知が走り、未初期化のフィールドや他インスタンスへアクセスして
`NullReferenceException` の原因になります。

初期化が必要なクラスは `IInitializable` を実装し、コンストラクタでは自分自身の登録と、自分だけで完結する初期化だけを行います。
実際の初期化処理は `Initialize()` に書き、`InitializableRegistry` から起動時に実行します。

```csharp
public partial class ExampleViewModel : ViewModelBase, IInitializable
{
    public ExampleViewModel()
    {
        // コンストラクタでは、自分自身や、このクラスが所有するオブジェクトだけを触る
        InitializableRegistry.Register(0, this);
    }

    public Task Initialize()
    {
        // 他インスタンスの参照・イベント購読・WhenAnyValue はここで行う
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => ApplyFilter());

        InstanceRepository.Items.OnUpdated += OnItemsUpdated;

        return Task.CompletedTask;
    }
}
```

- `Initialize()` は `InitializableRegistry.Complete()` から優先度の小さい順に呼ばれます。
  `Complete()` は `App.OnFrameworkInitializationCompleted()` で `MainWindow` を作成した後に一度だけ実行されます。
  そのため、起動完了後に生成されるインスタンスを登録しても初期化されません。動的に生成する場合は、
  生成直後に `Initialize()` を呼ぶなどの方法を使ってください。
- すべての `Initialize()` が終わってから実行したい処理は `IPostInitializable` を実装し、`OnInitialized()` に書きます。
  両方を実装する場合は、コンストラクタで両方を登録してください。

  ```csharp
  InitializableRegistry.Register(0, (IInitializable)this);
  InitializableRegistry.Register(0, (IPostInitializable)this);
  ```

- 登録は `InitializableRegistry.Register(priority, this)` で行います。現在の目安は、基盤となる
  `MainWindowViewModel` が `-1`、通常の ViewModel / View が `0`、最後に実行したい処理が大きい値（例: `9999`）です。
- コンストラクタで安全に行えるのは、コマンド・コレクションの初期化、自分が所有する子オブジェクトの生成、
  `Instance = this` のような自身のシングルトン登録、`InitializableRegistry` への登録です。

参考実装:

- `AvatarExplorer.UI/ViewModels/MainWindowViewModel.cs`
- `AvatarExplorer.UI/ViewModels/Panels/AdvancedSearchViewModel.cs`
- `AvatarExplorer.UI/Views/Panels/SidePanel/BulkImport.axaml.cs`

#### 初期化後の処理（IPostInitializable / OnInitialized）

`Initialize()` は他のインスタンスを参照しながら順番に実行されますが、すべての `Initialize()` が終わってから
初めて成立する処理もあります。その場合は `IPostInitializable` を実装し、`OnInitialized()` に書きます。

- `OnInitialized()` は、登録されたすべての `Initialize()` が完了した後に、`IPostInitializable` として
  登録した優先度の小さい順で呼ばれます。そのため「他の ViewModel の初期化結果を使う」
  「起動直後に一度だけ一覧を更新する」「初期セットアップを表示する」といった処理に向いています。
- `IInitializable` と同様に、コンストラクタで両方のインターフェースを登録します。

  ```csharp
  public partial class ExampleViewModel : ViewModelBase, IInitializable, IPostInitializable
  {
      public ExampleViewModel()
      {
          InitializableRegistry.Register(0, (IInitializable)this);
          InitializableRegistry.Register(0, (IPostInitializable)this);
      }

      public Task Initialize()
      {
          // 他インスタンスの参照やイベント購読
          return Task.CompletedTask;
      }

      public Task OnInitialized()
      {
          // すべての Initialize() が終わった後に一度だけ実行したい処理
          Refresh();
          return Task.CompletedTask;
      }
  }
  ```

- `Complete()` は `OnInitialized()` を順番に `await` するため、ここで重い処理を書くと起動完了が遅れます。
  起動をブロックしたくない処理は、`OnInitialized()` 内で `_ = SomeAsyncMethod();` のように明示的に開始し、
  例外が握りつぶされないようにしてください。
- `OnInitialized()` も起動時に一度だけ実行されます。起動完了後に生成されるインスタンスでは呼ばれないため、
  その場合は生成側で明示的に呼び出してください。
- `Complete()` の完了後、`App.OnFrameworkInitializationCompleted()` でアプリ引数が渡されます。
  引数に依存する処理は `OnInitialized()` では実行しないでください。

参考実装:

- `AvatarExplorer.UI/ViewModels/MainViewModel.cs` … すべての初期化後に一覧を `Refresh()` する
- `AvatarExplorer.UI/ViewModels/Overlays/InitialSetupViewModel.cs` … 初期セットアップが必要か判定して表示する
- `AvatarExplorer.UI/ViewModels/MainWindowViewModel.cs` … 移行処理や更新チェックを起動後に実行する

#### そのほかのUI実装の習慣

現在の `AvatarExplorer.UI` で見られる代表的な習慣です。新規コードではこれらに合わせてください。

| 項目 | 習慣 | 例 |
|---|---|---|
| ViewModel | `ViewModelBase`（`ReactiveObject`）を継承する | `MainViewModel` |
| 初期化後の処理 | すべての `Initialize()` が終わった後に実行したい処理は `IPostInitializable.OnInitialized()` に書く | `InitialSetupViewModel` |
| プロパティ | `[Reactive] public partial ...` を使い、手書きの `RaiseAndSetIfChanged` は避ける | `AdvancedSearchViewModel` |
| コマンド | `IReactiveCommand` と `ReactiveCommand.Create` / `CreateFromTask` を使う | `MainViewModel` |
| インスタンス参照 | DI コンテナは使わず、UI からは `InstanceRepository`、Core からは `AvatarExplorerApp.Instance` など各クラスの `static Instance` を使う。サービスは `sealed` + private コンストラクタの形が多い | `InstanceRepository` / `UserPreferencesService` |
| ローカライゼーション | UI 文字列は `Loc.*` と `Localizer.Instance[...]` を使う。XAML では `Source={x:Static loc:Localizer.Instance}` | `BulkImport.axaml` |
| 設定 | `UserPreferences` は `record` + `init`。`with` で複製し、`UserPreferencesRepository.Update` / `UpdateWithoutNotify` で保存する | `SettingsViewModel` |
| オーバーレイ | `ViewModels/Overlays` と `Views/Overlays` をペアで作る。`ShowAsync` と `TaskCompletionSource` で結果を返す | `YesNoDialogViewModel` |
| エラー | ユーザー通知が必要なものは `ErrorManager.Instance.PostError`、内部エラーは `PostInternalError` | `ErrorManager` |
| スレッド | バックグラウンドから UI を更新するときは `Dispatcher.UIThread.Post` / `InvokeAsync` を使う | `MainWindowViewModel` |
| 非同期処理 | イベントハンドラ以外は `async void` を避け、`Task` / `Task<T>` を返す | `MainViewModel.HandleUnitypackage` |
| View のコードビハインド | イベントハンドラや View 固有の処理のみを書き、ロジックは ViewModel に置く | `MainView.axaml.cs` |
| Core/UI 分離 | `AvatarExplorer.Core` は UI / Avalonia に依存させない | `AvatarExplorer.Core.csproj` |
| XAML | コンパイル済みバインディング（`x:DataType`）、色は `DynamicResource`、状態は `Classes` を使う。詳細は [Avalonia Style / Template 入門ガイド](./docs/ui-development/avalonia-style-guide.md) | — |

### ローカライゼーション

- ローカライゼーションファイル（`AvatarExplorer.Core/Data/Localization/*.json`）とローカライゼーションキー用のクラス（`AvatarExplorer.Core/Localization/LocalizationKeys.g.cs`）は `AvatarExplorer.Core` にあります。
- これらはお好きに使ってもらって構いません。UI には依存しておらず、そこに `Localization` があるだけです。

### AI 利用時のガイドライン

GitHub Copilot などの AI ツールを使うことは問題ありませんが、生成結果は必ず人の目でレビュー・検証してください。

確認ポイント:
- ロジックが正しいか
- エッジケースを考慮しているか
- プロジェクトのコーディング規約に準拠しているか
- パフォーマンス上の問題がないか

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
- If you added startup initialization, confirm it follows the `IInitializable` / `InitializableRegistry` convention

### UI Code Conventions

#### Initialization (IInitializable / InitializableRegistry)

In `AvatarExplorer.UI`, do not access other instances whose availability depends on startup order from a
ViewModel or View constructor. The construction order of separate instances is not guaranteed, so singletons
such as `InstanceRepository.MainWindow` or `MainWindowViewModel.Instance` may still be `null`. Subscribing with
`this.WhenAnyValue(...)` in a constructor can also fire before the object is fully constructed, causing
`NullReferenceException` when the handler touches uninitialized fields or other instances.

Implement `IInitializable` on classes that need initialization. The constructor should only register the instance
and perform initialization that is self-contained. Put the actual initialization in `Initialize()` and let
`InitializableRegistry` run it during startup.

```csharp
public partial class ExampleViewModel : ViewModelBase, IInitializable
{
    public ExampleViewModel()
    {
        // In the constructor, only touch this object or objects it owns.
        InitializableRegistry.Register(0, this);
    }

    public Task Initialize()
    {
        // Access other instances, subscribe to events, and set up WhenAnyValue here.
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => ApplyFilter());

        InstanceRepository.Items.OnUpdated += OnItemsUpdated;

        return Task.CompletedTask;
    }
}
```

- `Initialize()` is called by `InitializableRegistry.Complete()` in ascending priority order.
  `Complete()` runs once from `App.OnFrameworkInitializationCompleted()` after the `MainWindow` is created.
  Instances created after startup completes will therefore not be initialized through the registry; when creating
  them dynamically, call `Initialize()` directly or use another explicit initialization path.
- If work must run after every `Initialize()` has completed, implement `IPostInitializable` and put it in `OnInitialized()`.
  Register both when a class implements both interfaces.

  ```csharp
  InitializableRegistry.Register(0, (IInitializable)this);
  InitializableRegistry.Register(0, (IPostInitializable)this);
  ```

- Register with `InitializableRegistry.Register(priority, this)`. The current guideline is `-1` for the foundational
  `MainWindowViewModel`, `0` for normal ViewModels / Views, and a large value (e.g. `9999`) for work that should run last.
- The constructor may safely initialize commands and collections, create child objects it owns, assign its own
  singleton (`Instance = this`), and register with `InitializableRegistry`.

Reference implementations:

- `AvatarExplorer.UI/ViewModels/MainWindowViewModel.cs`
- `AvatarExplorer.UI/ViewModels/Panels/AdvancedSearchViewModel.cs`
- `AvatarExplorer.UI/Views/Panels/SidePanel/BulkImport.axaml.cs`

#### Post-initialization (IPostInitializable / OnInitialized)

`Initialize()` methods run in order while other instances are being initialized. Some work is only valid after
every `Initialize()` has completed. Implement `IPostInitializable` and put that work in `OnInitialized()`.

- `OnInitialized()` is called after all registered `Initialize()` methods have completed, in ascending order of
  the `IPostInitializable` priority. It is suitable for work such as using the results of another ViewModel's
  initialization, refreshing a list once at startup, or opening the initial setup dialog.
- Register both interfaces in the constructor, just like `IInitializable`.

  ```csharp
  public partial class ExampleViewModel : ViewModelBase, IInitializable, IPostInitializable
  {
      public ExampleViewModel()
      {
          InitializableRegistry.Register(0, (IInitializable)this);
          InitializableRegistry.Register(0, (IPostInitializable)this);
      }

      public Task Initialize()
      {
          // Access other instances and subscribe to events.
          return Task.CompletedTask;
      }

      public Task OnInitialized()
      {
          // Work that should run once after every Initialize() has completed.
          Refresh();
          return Task.CompletedTask;
      }
  }
  ```

- `Complete()` awaits each `OnInitialized()` in order, so heavy work here delays startup completion. If work should
  not block startup, start it explicitly with `_ = SomeAsyncMethod();` inside `OnInitialized()` and make sure
  exceptions are not swallowed.
- `OnInitialized()` also runs only once at startup. Instances created after startup completes will not receive the
  call, so invoke it explicitly from the creation path in that case.
- Application arguments are passed by `App.OnFrameworkInitializationCompleted()` after `Complete()` finishes.
  Do not perform argument-dependent work in `OnInitialized()`.

Reference implementations:

- `AvatarExplorer.UI/ViewModels/MainViewModel.cs` … refreshes the item list after all initialization is done
- `AvatarExplorer.UI/ViewModels/Overlays/InitialSetupViewModel.cs` … decides whether to show the initial setup dialog
- `AvatarExplorer.UI/ViewModels/MainWindowViewModel.cs` … runs migrations and the startup update check

#### Other UI implementation habits

These are the representative habits found in the current `AvatarExplorer.UI`. Follow them for new code.

| Area | Habit | Example |
|---|---|---|
| ViewModel | Inherit from `ViewModelBase` (`ReactiveObject`) | `MainViewModel` |
| Post-initialization | Put work that must run after every `Initialize()` in `IPostInitializable.OnInitialized()` | `InitialSetupViewModel` |
| Properties | Use `[Reactive] public partial ...`; avoid manual `RaiseAndSetIfChanged` | `AdvancedSearchViewModel` |
| Commands | Use `IReactiveCommand` with `ReactiveCommand.Create` / `CreateFromTask` | `MainViewModel` |
| Instance access | No DI container; use `InstanceRepository` from the UI and `AvatarExplorerApp.Instance` or another class's `static Instance` from Core. Services are often `sealed` with a private constructor | `InstanceRepository` / `UserPreferencesService` |
| Localization | Use `Loc.*` and `Localizer.Instance[...]`; in XAML use `Source={x:Static loc:Localizer.Instance}` | `BulkImport.axaml` |
| Settings | `UserPreferences` is a `record` with `init`; copy with `with` and save via `UserPreferencesRepository.Update` / `UpdateWithoutNotify` | `SettingsViewModel` |
| Overlays | Create `ViewModels/Overlays` and `Views/Overlays` as a pair; return results through `ShowAsync` and `TaskCompletionSource` | `YesNoDialogViewModel` |
| Errors | Use `ErrorManager.Instance.PostError` for user-facing errors and `PostInternalError` for internal errors | `ErrorManager` |
| Threading | Use `Dispatcher.UIThread.Post` / `InvokeAsync` when updating UI from a background thread | `MainWindowViewModel` |
| Async | Avoid `async void` outside event handlers; return `Task` / `Task<T>` | `MainViewModel.HandleUnitypackage` |
| View code-behind | Keep only event handlers and view-specific work; put logic in the ViewModel | `MainView.axaml.cs` |
| Core/UI separation | `AvatarExplorer.Core` must not depend on the UI / Avalonia | `AvatarExplorer.Core.csproj` |
| XAML | Use compiled bindings (`x:DataType`), `DynamicResource` for colors, and `Classes` for state. See the [Avalonia Style / Template guide](./docs/ui-development/avalonia-style-guide.md) | — |

### Localization

- The localization files (`AvatarExplorer.Core/Data/Localization/*.json`) and the localization key class (`AvatarExplorer.Core/Localization/LocalizationKeys.g.cs`) live in `AvatarExplorer.Core`.
- Feel free to use them however you like. They do not depend on the UI — `Localization` is simply there.

### AI usage guidelines

Using AI tools such as GitHub Copilot is allowed, but generated results must always be reviewed and validated by a human.

Review points:
- Is the logic correct?
- Are edge cases considered?
- Does it follow the project's coding conventions?
- Are there performance concerns?
