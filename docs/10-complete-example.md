# 10 - 完全な実装例

この章では、AvatarExplorer.Coreを使ったコンソールアプリケーションの完全な実装例を紹介します。

## シンプルなナビゲーションアプリ

```csharp
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;

class Program
{
    static async Task Main(string[] args)
    {
        // 初期化
        var app = AvatarExplorerApp.Instance;
        app.Initialize();

        Console.WriteLine("AvatarExplorer コンソールクライアント");
        Console.WriteLine($"バージョン: {AvatarExplorerApp.CurrentVersion}");
        Console.WriteLine();

        var navigation = app.ItemNavigationService;

        // ファイル選択イベントの購読
        navigation.FileOpenRequested += path =>
        {
            Console.WriteLine($"\n[ファイル選択] {path}");
            Console.WriteLine("このファイルを開きますか？ (y/n)");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
            }
            Console.WriteLine();
        };

        // メインループ
        while (true)
        {
            await DisplayCurrentView(navigation);

            Console.Write("\nコマンド (番号, u=戻る, c=クリア, s=検索, q=終了): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;

            switch (input.ToLower())
            {
                case "q":
                    return;
                case "u":
                    navigation.Undo();
                    break;
                case "c":
                    navigation.Clear();
                    break;
                case "s":
                    await HandleSearch(navigation, app);
                    break;
                default:
                    if (int.TryParse(input, out int index))
                    {
                        await HandleSelection(navigation, index);
                    }
                    break;
            }
        }
    }

    static Task DisplayCurrentView(ItemNavigationService navigation)
    {
        // パンくずリストの表示
        var nodes = navigation.GetCurrentSelectionNodes().ToList();
        if (nodes.Count > 0)
        {
            Console.Write("現在位置: ");
            foreach (var node in nodes)
            {
                Console.Write($"{node.Value} > ");
            }
            Console.WriteLine();
        }

        // 現在のビューの表示
        var viewItems = navigation.GetCurrentSelectionView();

        Console.WriteLine($"\n=== {viewItems.Length}件 ===\n");

        for (int i = 0; i < viewItems.Length && i < 20; i++)
        {
            var item = viewItems[i];
            DisplayItem(item, i + 1);
        }

        if (viewItems.Length > 20)
        {
            Console.WriteLine($"... 他 {viewItems.Length - 20}件");
        }

        return Task.CompletedTask;
    }

    static void DisplayItem(IIdentifiable item, int index)
    {
        switch (item)
        {
            case Item i:
                Console.WriteLine($"{index,3}. [{i.Category}] {i.Title} by {i.Author}");
                break;
            case Folder f:
                Console.WriteLine($"{index,3}. [フォルダ] {f.Title} ({f.ItemCount}件)");
                break;
            case ItemFile file:
                Console.WriteLine($"{index,3}. [ファイル] {file.FileName}");
                break;
            case Author a:
                Console.WriteLine($"{index,3}. [作者] {a.Name} ({a.ItemCount}件)");
                break;
            case Avatar av:
                var name = av.Item switch
                {
                    Item avatarItem => avatarItem.Title,
                    CommonAvatar ca => $"[共通] {ca.GroupName}",
                    TempAvatar ta => $"[仮] {ta.AvatarName}",
                    _ => "Unknown"
                };
                Console.WriteLine($"{index,3}. [アバター] {name}");
                break;
            default:
                Console.WriteLine($"{index,3}. {item.Identifier}");
                break;
        }
    }

    static async Task HandleSelection(ItemNavigationService navigation, int index)
    {
        var viewItems = navigation.GetCurrentSelectionView();
        if (index < 1 || index > viewItems.Length)
        {
            Console.WriteLine("無効な番号です");
            return;
        }

        var selected = viewItems[index - 1];
        navigation.Select(selected.Identifier);
    }

    static async Task HandleSearch(ItemNavigationService navigation, AvatarExplorerApp app)
    {
        Console.Write("検索クエリ: ");
        var query = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(query)) return;

        var results = app.ItemGroupService.SearchItems(query, SearchResultTypes.Items);

        Console.WriteLine($"\n検索結果: {results.Length}件\n");

        for (int i = 0; i < results.Length && i < 10; i++)
        {
            var item = app.ItemRepository.Get(results[i]);
            if (item != null)
            {
                Console.WriteLine($"{i + 1}. [{item.Category}] {item.Title} by {item.Author}");
            }
        }

        if (results.Length > 0)
        {
            Console.Write("\n番号を選択して移動 (Enterでキャンセル): ");
            var input = Console.ReadLine();

            if (int.TryParse(input, out int index) && index >= 1 && index <= results.Length)
            {
                navigation.Clear();
                navigation.Select(results[index - 1]);
            }
        }
    }
}
```

## Booth連携アプリ

```csharp
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;

class BoothClient
{
    private readonly AvatarExplorerApp _app;

    public BoothClient()
    {
        _app = AvatarExplorerApp.Instance;
        _app.Initialize();
    }

    public async Task RegisterFromBooth(string boothUrl)
    {
        Console.WriteLine($"Boothから取得中: {boothUrl}");

        var result = await BoothService.Fetch(boothUrl, includeVariations: true);

        if (result.IsError)
        {
            Console.WriteLine($"エラー: {result.Errors}");
            return;
        }

        var boothItem = result.Value;

        // 既存チェック
        var existing = _app.ItemRepository.GetAll()
            .FirstOrDefault(i => i.BoothId == boothItem.BoothId);

        if (existing != null)
        {
            Console.WriteLine($"既に登録済み: {existing.Title}");
            return;
        }

        // 商品情報の表示
        Console.WriteLine($"\n商品名: {boothItem.Title}");
        Console.WriteLine($"ショップ: {boothItem.Shop.Name}");
        Console.WriteLine($"カテゴリ: {boothItem.Category.Name}");
        Console.WriteLine($"推定タイプ: {boothItem.EstimatedCategory.Type}");

        if (boothItem.Variations.Length > 0)
        {
            Console.WriteLine("\nバリエーション:");
            foreach (var v in boothItem.Variations)
            {
                Console.WriteLine($"  - {v.Name}: {v.Price}円");
            }
        }

        // 登録確認
        Console.Write("\n登録しますか？ (y/n): ");
        if (Console.ReadKey().Key != ConsoleKey.Y) return;
        Console.WriteLine();

        // アイテム作成
        var context = new ItemCreationContext
        {
            Title = boothItem.Title,
            Author = boothItem.Shop.Name,
            AuthorId = boothItem.Shop.Id,
            BoothId = boothItem.BoothId,
            ItemType = boothItem.EstimatedCategory.Type,
            ThumbnailUrl = boothItem.ThumbnailUrl,
            IsHidden = false,
            SkipIndirectCommonAvatarCheck = false
        };

        var newItem = await _app.ItemRepository.Create(context);

        Console.WriteLine($"登録完了: {newItem.Title}");
        Console.WriteLine($"Identifier: {newItem.Identifier}");
    }

    public async Task BatchRegister(IEnumerable<string> urls)
    {
        int success = 0, failed = 0, skipped = 0;

        foreach (var url in urls)
        {
            try
            {
                var result = await BoothService.Fetch(url);

                if (result.IsError)
                {
                    Console.WriteLine($"[失敗] {url}");
                    failed++;
                    continue;
                }

                var boothItem = result.Value;

                var existing = _app.ItemRepository.GetAll()
                    .FirstOrDefault(i => i.BoothId == boothItem.BoothId);

                if (existing != null)
                {
                    Console.WriteLine($"[スキップ] {boothItem.Title} (既に登録済み)");
                    skipped++;
                    continue;
                }

                var context = new ItemCreationContext
                {
                    Title = boothItem.Title,
                    Author = boothItem.Shop.Name,
                    AuthorId = boothItem.Shop.Id,
                    BoothId = boothItem.BoothId,
                    ItemType = boothItem.EstimatedCategory.Type,
                    ThumbnailUrl = boothItem.ThumbnailUrl,
                    IsHidden = false,
                    SkipIndirectCommonAvatarCheck = false
                };

                await _app.ItemRepository.Create(context);
                Console.WriteLine($"[成功] {boothItem.Title}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[エラー] {url}: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"\n完了: 成功={success}, 失敗={failed}, スキップ={skipped}");
    }
}
```

## アイテム管理アプリ

```csharp
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;

class ItemManager
{
    private readonly AvatarExplorerApp _app;

    public ItemManager()
    {
        _app = AvatarExplorerApp.Instance;
        _app.Initialize();
    }

    public void ListItems(string? filter = null)
    {
        var items = _app.ItemRepository.GetAll();

        if (!string.IsNullOrEmpty(filter))
        {
            var searchResults = _app.ItemGroupService.SearchItems(filter, SearchResultTypes.Items);
            items = items.Where(i => searchResults.Contains(i.Identifier)).ToList();
        }

        Console.WriteLine($"アイテム一覧: {items.Count}件\n");

        foreach (var item in items.OrderBy(i => i.Title))
        {
            Console.WriteLine($"[{item.Category}] {item.Title}");
            Console.WriteLine($"  作者: {item.Author}");
            Console.WriteLine($"  ID: {item.Identifier}");
            Console.WriteLine($"  Booth: {(item.BoothId != -1 ? item.BoothId.ToString() : "なし")}");
            Console.WriteLine();
        }
    }

    public async Task AddItem(string title, string author, ItemType type, string? boothUrl = null)
    {
        var context = new ItemCreationContext
        {
            Title = title,
            Author = author,
            ItemType = type
        };

        if (!string.IsNullOrEmpty(boothUrl))
        {
            var boothResult = await BoothService.Fetch(boothUrl);
            if (!boothResult.IsError)
            {
                var boothItem = boothResult.Value;
                context.BoothId = boothItem.BoothId;
                context.AuthorId = boothItem.Shop.Id;
                context.ThumbnailUrl = boothItem.ThumbnailUrl;
            }
        }

        var item = await _app.ItemRepository.Create(context);
        Console.WriteLine($"作成完了: {item.Identifier}");
    }

    /// <summary>
    /// アイテムにコンテンツ（ファイル・フォルダ・URL）を追加します。
    /// ローカルファイルはコピー、アーカイブは自動展開、URLはダウンロード後に処理されます。
    /// </summary>
    public async Task AddContentsToItem(string itemId, IEnumerable<string> filePaths)
    {
        // パスの種類（ファイル/フォルダ/URL）に応じてItemContentEntryを作成
        var contents = filePaths.Select(p => new ItemContentEntry
        {
            FileName = Path.GetFileName(p),
            Path = p,
            IsUrl = p.StartsWith("http")  // URLの場合はtrue
        }).ToList();

        Console.WriteLine($"{contents.Count}個のコンテンツを追加中...");

        // AddContents: ファイルコピー、アーカイブ展開、URLダウンロードをまとめて処理
        var result = await _app.ItemRepository.AddContents(
            itemId,
            contents,
            shouldLinkToOriginal: _app.RuntimeSettings.ShouldLinkToOriginal,  // フォルダをリンクするか
            removeOriginal: _app.RuntimeSettings.RemoveOriginal,              // 展開後に元ファイルを削除するか
            reportProgress: p =>
            {
                Console.Write($"\r{p.Message}: {p.Percent}%");
                return Task.CompletedTask;
            }
        );

        Console.WriteLine();

        if (result.IsError)
        {
            Console.WriteLine($"エラー: {result.Errors}");
        }
        else
        {
            Console.WriteLine($"コンテンツ追加完了（アイテムフォルダ: {result.Value.ItemParentFolder}）");
        }
    }

    public void RemoveItem(string itemId)
    {
        var item = _app.ItemRepository.Get(itemId);
        if (item == null)
        {
            Console.WriteLine("アイテムが見つかりません");
            return;
        }

        Console.WriteLine($"削除: {item.Title}");
        Console.Write("本当に削除しますか？ (y/n): ");

        if (Console.ReadKey().Key == ConsoleKey.Y)
        {
            _app.ItemGroupService.RemoveItem(itemId, removeFolder: false);
            Console.WriteLine("\n削除完了");
        }
    }

    public async Task PrepareUnitypackage(string itemId)
    {
        var item = _app.ItemRepository.Get(itemId);
        if (item == null)
        {
            Console.WriteLine("アイテムが見つかりません");
            return;
        }

        var files = _app.ItemRepository.EnumerateItemFiles(itemId);
        var unitypackages = files.Where(f =>
            f.Extension.Equals("unitypackage", StringComparison.OrdinalIgnoreCase)).ToList();

        if (unitypackages.Count == 0)
        {
            Console.WriteLine("Unitypackageが見つかりません");
            return;
        }

        var entries = unitypackages.Select(f => new UnitypackageImportEntry
        {
            FilePath = f.FilePath,
            CategoryDisplayName = item.Category.ToString()
        }).ToList();

        Console.WriteLine($"{entries.Count}個のUnitypackageを処理中...");

        var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(new UnitypackageModifyRequest
        {
            Entries = entries,
            ReportProgress = p =>
            {
                Console.Write($"\r{p.Message}: {p.Percent}%");
                return Task.CompletedTask;
            }
        });

        Console.WriteLine();

        if (!result.IsError && result.ModifiedUnitypackagePath != null)
        {
            Console.WriteLine($"準備完了: {result.ModifiedUnitypackagePath}");

            if (result.ContainsScripts)
            {
                Console.WriteLine("警告: C#スクリプトが含まれています");
            }
        }
    }

    public async Task ExportData(string outputPath, DataExportType exportType)
    {
        Console.WriteLine("エクスポート中...");

        var result = await _app.ItemGroupService.Export(
            exportType,
            outputPath,
            type => new ValueTask<string?>(type.ToString()),
            includeCommonToSupported: true,
            reportProgress: async progress => Console.Write($"\r{progress.Item1}: {progress.Item2}%")
        );

        Console.WriteLine();

        if (result.IsError)
        {
            Console.WriteLine("エクスポート失敗");
        }
        else
        {
            Console.WriteLine("エクスポート完了");
        }
    }
}
```

## 設定管理アプリ

```csharp
using AvatarExplorer.Core.Services.System;

class SettingsManager
{
    private readonly AvatarExplorerApp _app;

    public SettingsManager()
    {
        _app = AvatarExplorerApp.Instance;
        _app.Initialize();
    }

    public void ShowSettings()
    {
        var settings = _app.RuntimeSettings;

        Console.WriteLine("=== 現在の設定 ===\n");
        Console.WriteLine($"データ保存先: {settings.DataRootDirectory}");
        Console.WriteLine($"バックアップ先: {settings.AutoBackupRootDirectory}");
        Console.WriteLine($"バックアップ間隔: {settings.AutoBackupInterval}日");
        Console.WriteLine($"最大並列数: {settings.MaxDegreeOfParallelism}");
        Console.WriteLine($"元ファイルを削除: {settings.RemoveOriginal}");
        Console.WriteLine($"元ファイルへリンク: {settings.ShouldLinkToOriginal}");
        Console.WriteLine($"Unitypackageパス自動変更: {settings.AutoChangeUnitypackagePath}");
        Console.WriteLine($"アップデートチェック: {settings.CheckForUpdate}");
        Console.WriteLine($"アップデートチャンネル: {settings.UpdateChannel}");
    }

    public void UpdateParallelism(int value)
    {
        var settings = _app.RuntimeSettings;
        _app.RuntimeSettingsRepository.Update(settings with
        {
            MaxDegreeOfParallelism = Math.Clamp(value, 1, 16)
        });
        Console.WriteLine($"最大並列数を {value} に変更しました");
    }

    public void UpdateBackupInterval(int days)
    {
        var settings = _app.RuntimeSettings;
        _app.RuntimeSettingsRepository.Update(settings with
        {
            AutoBackupInterval = Math.Clamp(days, 1, 365)
        });
        Console.WriteLine($"バックアップ間隔を {days}日 に変更しました");
    }

    public void UpdateDataRoot(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var settings = _app.RuntimeSettings;
        _app.RuntimeSettingsRepository.Update(settings with
        {
            DataRootDirectory = path
        });
        Console.WriteLine($"データ保存先を {path} に変更しました");
    }
}
```

## プロジェクトファイルの例

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

## まとめ

このチュートリアルでは、AvatarExplorer.Coreを使ったクライアントアプリケーションの開発方法を学びました。

### 主要なポイント

1. **初期化**: `AvatarExplorerApp.Instance.Initialize()`を最初に呼び出す
2. **ナビゲーション**: `ItemNavigationService`でIdentifierベースの選択操作
3. **アイテム管理**: `ItemRepository`でCRUD操作
4. **安全な削除**: `ItemGroupService.RemoveItem()`を使用
5. **Booth連携**: `BoothService.Fetch()`で商品情報取得
6. **Unitypackage処理**: `FileSystemService.ModifyUnitypackageFilePathsAsync()`
7. **設定管理**: `RuntimeSettingsRepository.Update()`で設定変更
8. **検索**: `ItemGroupService.SearchItems()`で高速検索

### 参考リンク

- [AvatarExplorer リポジトリ](https://github.com/puk06/VRC-Avatar-Explorer)
- [AvatarExplorer.Core ソースコード](../AvatarExplorer.Core/)

ご質問や問題があれば、Issueを作成してください。
