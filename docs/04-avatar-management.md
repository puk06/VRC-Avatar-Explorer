# 04 - 共通素体・仮アバターの管理

この章では、`CommonAvatarRepository`と`TempAvatarRepository`を使った共通素体グループと仮アバターの管理について学びます。

## 共通素体（CommonAvatar）とは

共通素体は、複数のアバターをグループ化するための機能です。例えば、「素体が同じアバター群」を一つのグループとして扱うことができます。

```
共通素体グループ「Aシリーズ」
├── アバターA-1
├── アバターA-2
└── アバターA-3
```

このグループに対応アバターを設定すると、グループ内の全てのアバターに対応することになります。

## CommonAvatarRepository

### 取得

```csharp
var app = AvatarExplorerApp.Instance;
app.Initialize();

var commonAvatarRepo = app.CommonAvatarRepository;
```

### Create() - 共通素体グループの作成

```csharp
commonAvatarRepo.Create("Aシリーズ共通素体");
```

### GetAll() - 全グループの取得

```csharp
var allGroups = commonAvatarRepo.GetAll();

foreach (var group in allGroups)
{
    Console.WriteLine($"グループ名: {group.GroupName}");
    Console.WriteLine($"  ID: {group.Identifier}");
    Console.WriteLine($"  含まれるアバター数: {group.Avatars.Count}");
}
```

### Get() - 特定グループの取得

```csharp
var group = commonAvatarRepo.Get("commonavatar:xxxxx");

if (group != null)
{
    Console.WriteLine($"グループ名: {group.GroupName}");
}
```

### UpdateAvatars() - グループ内のアバターを更新

```csharp
var avatarIds = new[]
{
    "item:avatar-id-1",
    "item:avatar-id-2",
    "item:avatar-id-3"
};

commonAvatarRepo.UpdateAvatars("commonavatar:xxxxx", avatarIds);
```

### RenameGroup() - グループ名の変更

```csharp
commonAvatarRepo.RenameGroup("commonavatar:xxxxx", "新しいグループ名");
```

### Remove() - グループの削除

**重要**: 共通素体グループの削除には、`ItemGroupService.RemoveCommonAvatar()`を使用してください。

```csharp
// 正しい方法
app.ItemGroupService.RemoveCommonAvatar(
    "commonavatar:xxxxx",
    replaceToAvatars: true  // true: グループ内のアバターを個別の対応アバターに置換
);
```

`replaceToAvatars`パラメータ：
- `true`: グループ内の各アバターを、アイテムの対応アバターに個別に追加
- `false`: グループへの参照のみを削除

## CommonAvatarモデル

```csharp
public class CommonAvatar : IIdentifiable
{
    public string GroupName { get; }              // グループ名
    public ImmutableArray<string> Avatars { get; } // 含まれるアバターID一覧
    public string Identifier { get; }             // "commonavatar:" + Id
}
```

## 仮アバター（TempAvatar）とは

仮アバターは、まだ正式に登録されていないアバターを一時的に識別するための機能です。後で正式なアバターに「解決」することができます。

## TempAvatarRepository

### 取得

```csharp
var tempAvatarRepo = app.TempAvatarRepository;
```

### Create() - 仮アバターの作成

```csharp
var tempAvatar = tempAvatarRepo.Create("仮アバター名");
Console.WriteLine($"作成された仮アバター: {tempAvatar.Identifier}");
```

### GetAll() - 全仮アバターの取得

```csharp
var allTempAvatars = tempAvatarRepo.GetAll();

foreach (var temp in allTempAvatars)
{
    Console.WriteLine($"{temp.AvatarName}: {temp.Identifier}");
}
```

### Get() - 特定仮アバターの取得

```csharp
var temp = tempAvatarRepo.Get("tempavatar:xxxxx");
```

### Remove() - 仮アバターの削除

**重要**: 仮アバターの削除には、`ItemGroupService.RemoveTempAvatar()`を使用してください。

```csharp
app.ItemGroupService.RemoveTempAvatar("tempavatar:xxxxx");
```

### ResolveTempAvatar() - 仮アバターの解決

仮アバターを正式なアバターに置換します。

```csharp
app.ItemGroupService.ResolveTempAvatar(
    tempAvatarId: "tempavatar:xxxxx",
    targetItemId: "item:正式なアバターID"
);
```

この処理では：
1. 全アイテムの`SupportedAvatars`から仮アバターIDを正式なアバターIDに置換
2. 全共通素体グループの`Avatars`から仮アバターIDを正式なアバターIDに置換
3. 仮アバターを削除

## TempAvatarモデル

```csharp
public class TempAvatar : IIdentifiable
{
    public string AvatarName { get; }  // アバター名
    public string Identifier { get; }  // "tempavatar:" + Id
}
```

## アイテムの共通素体対応判定（AvatarStatusResolver）

アイテムが共通素体に対応しているかどうかの判定は、`AvatarStatusResolver`で行われます。

### 判定ロジック

アイテムの対応アバターに、共通素体グループ内のアバターが**1つでも含まれている**場合、そのアイテムは「共通素体対応」として表示されます。

```
例: 共通素体グループ「Aシリーズ」に アバターA-1, A-2, A-3 が含まれている場合

アイテムXの対応アバター: [アバターA-1]
→ アバターA-2 を見ている時でも「共通素体: Aシリーズ」として表示される

アイテムYの対応アバター: [アバターA-1, アバターB]
→ アバターA-2 を見ている時でも「共通素体: Aシリーズ」として表示される
→ アバターB を見ている時は「対応あり」として表示される
```

### 実装の詳細

```csharp
var status = AvatarStatusResolver.Resolve(item, avatarId, commonAvatars);

// status.IsCommon == true の場合、共通素体対応
// status.CommonAvatarName にグループ名が設定される
```

判定は2段階で行われます：

1. **アイテムの対応アバターが共通素体グループの場合**
   - アイテムの`SupportedAvatars`に`commonavatar:xxxxx`が含まれている場合
   - そのグループ内に現在表示中のアバターが含まれていれば`IsCommon = true`

2. **間接的な共通素体チェック**（`SkipIndirectCommonAvatarCheck`が`true`の場合はスキップ）
   - 現在表示中のアバターが属する共通素体グループを検索
   - そのグループ内にアイテムの対応アバターのいずれかが含まれていれば`IsCommon = true`

> [!NOTE]
> `SkipIndirectCommonAvatarCheck`を有効にすると、2段階目の間接的な判定のみがスキップされます。1段階目の`commonavatar:xxxxx`を直接設定した場合の判定は常に実行されます。

## Avatarモデル

`Avatar`クラスは、通常のアイテム（アバター）、共通素体、仮アバターを統一的に扱うためのラッパークラスです。

```csharp
public class Avatar : IIdentifiable
{
    public AvatarType Type { get; }  // None, Item, CommonAvatar, TempAvatar
    public IIdentifiable Item { get; }  // ラップされているオブジェクト
    public bool RawIdentifier { get; }  // avatar:プレフィックスの有無
    public string Identifier { get; }  // "avatar:" + Item.Identifier
}
```

### 使用例

```csharp
// ItemGroupServiceからアバター一覧を取得
var avatars = app.ItemGroupService.GetAvatars(
    includeCommonAvatar: true,
    includeTempAvatar: true
);

foreach (var avatar in avatars)
{
    Console.WriteLine($"タイプ: {avatar.Type}");
    Console.WriteLine($"Identifier: {avatar.Identifier}");
    
    // 元のオブジェクトにアクセス
    switch (avatar.Item)
    {
        case Item item:
            Console.WriteLine($"  アバター名: {item.Title}");
            break;
        case CommonAvatar common:
            Console.WriteLine($"  グループ名: {common.GroupName}");
            break;
        case TempAvatar temp:
            Console.WriteLine($"  仮アバター名: {temp.AvatarName}");
            break;
    }
}
```

## GetQueryFilters()での取得

ナビゲーション用のフィルタとしてアバター一覧を取得するには：

```csharp
// QueryType.Avatarで、通常アバター + 共通素体 + 仮アバターを取得
var avatarFilters = app.ItemGroupService.GetQueryFilters(QueryType.Avatar);

foreach (var filter in avatarFilters)
{
    Console.WriteLine(filter.Identifier);
    // avatar:item:xxxxx (通常アバター)
    // avatar:commonavatar:xxxxx (共通素体)
    // avatar:tempavatar:xxxxx (仮アバター)
}
```

## GetItemsFromAvatar() - アバターからアイテムを取得

特定のアバターに対応するアイテム一覧を取得します。

```csharp
// 通常アバター
var items = app.ItemGroupService.GetItemsFromAvatar("item:avatar-id");

// 共通素体グループの場合も同様に
var itemsForGroup = app.ItemGroupService.GetItemsFromAvatar("commonavatar:group-id");
```

## GetAllSupportedAvatarsIds() - 対応アバターIDの展開

共通素体グループを展開して、全ての対応アバターIDを取得します。

```csharp
var avatarIds = new[]
{
    "item:avatar-1",
    "commonavatar:group-1"  // この中にavatar-2, avatar-3が含まれている
};

var expandedIds = app.ItemGroupService.GetAllSupportedAvatarsIds(
    avatarIds,
    includeCommonAvatarToSupported: true  // 共通素体も展開
);

// 結果: ["item:avatar-1", "item:avatar-2", "item:avatar-3"]
```

## ReplaceSupportedAvatarsToCommonAvatarGroup()

衣類アイテムの対応アバターを、共通素体グループに置換します。

```csharp
// 事前に共通素体グループを作成しておく
commonAvatarRepo.Create("共通素体グループ");
commonAvatarRepo.UpdateAvatars("commonavatar:xxxxx", new[]
{
    "item:avatar-1",
    "item:avatar-2"
});

// 衣類アイテムの対応アバターを共通素体グループに置換
app.ItemGroupService.ReplaceSupportedAvatarsToCommonAvatarGroup("commonavatar:xxxxx");
```

## 実践的な例

### 例1: 共通素体グループの作成と管理

```csharp
var app = AvatarExplorerApp.Instance;
app.Initialize();

// 1. 共通素体グループの作成
app.CommonAvatarRepository.Create("VRChat公式アバター");

// 2. 作成したグループを取得
var group = app.CommonAvatarRepository.GetAll()
    .FirstOrDefault(g => g.GroupName == "VRChat公式アバター");

if (group != null)
{
    // 3. グループにアバターを追加
    var officialAvatars = app.ItemRepository.GetAll()
        .Where(i => i.Category.Type == ItemType.Avatar && i.Author == "VRChat")
        .Select(i => i.Identifier)
        .ToArray();
    
    app.CommonAvatarRepository.UpdateAvatars(group.Identifier, officialAvatars);
    
    Console.WriteLine($"グループ「{group.GroupName}」に {officialAvatars.Length} 個のアバターを追加");
}
```

### 例2: 仮アバターの解決

```csharp
// 1. 仮アバターの作成
var tempAvatar = app.TempAvatarRepository.Create("新しいアバター（仮）");

// 2. 仮アバターをアイテムの対応アバターに設定
var item = app.ItemRepository.Get("item:xxxxx");
item.UpdateSupportedAvatars(item.SupportedAvatars.Append(tempAvatar.Identifier));
app.ItemRepository.Save();

// 3. 後で正式なアバターが決まった場合
var realAvatar = app.ItemRepository.Get("item:real-avatar-id");

// 4. 仮アバターを解決
app.ItemGroupService.ResolveTempAvatar(
    tempAvatar.Identifier,
    realAvatar.Identifier
);
// これで、itemの対応アバターが仮アバターから正式なアバターに置換される
```

### 例3: 共通素体の削除と置換

```csharp
var group = app.CommonAvatarRepository.Get("commonavatar:xxxxx");

if (group != null)
{
    // グループ内のアバターを個別の対応アバターに置換してから削除
    app.ItemGroupService.RemoveCommonAvatar(
        group.Identifier,
        replaceToAvatars: true
    );
    
    Console.WriteLine($"グループ「{group.GroupName}」を削除しました");
}
```

## 次のステップ

[05 - Booth連携](./05-booth-integration.md) では、Booth APIを使ったアイテム情報の取得について学びます。
