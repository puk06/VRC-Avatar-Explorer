# 11 - JSONユーティリティ

この章では、JSONファイルの読み取りに便利なユーティリティクラスについて学びます。

## JsonPathReader

`JsonPathReader`は、JSONファイルを簡単に読み取ってプロパティパスで値を取得するためのシンプルなユーティリティクラスです。

### 基本的な使い方

```csharp
using AvatarExplorer.Core.Services.IO;

// JSONファイルのパスを指定してリーダーを作成
var reader = new JsonPathReader(@"C:\path\to\data.json");

// ファイルを読み込む
var value = reader.Read();

// プロパティにアクセス
var name = value?["user"]["profile"]["name"] as string;
var age = value?["user"]["profile"]["age"] as int?;
```

### ドット区切りパスでの値取得

`TryGetPathValue<T>()`を使うと、ドット区切りのパスで値を取得できます。

```csharp
var reader = new JsonPathReader(@"C:\path\to\data.json");
var value = reader.Read();

// ドット区切りで深いプロパティにアクセス
if (value?.TryGetPathValue<string>("user.profile.name", out var name) == true)
{
    Console.WriteLine($"名前: {name}");
}

// 配列の要素にもアクセス可能（数値インデックスを使用）
if (value?.TryGetPathValue<string>("items.0.name", out var firstName) == true)
{
    Console.WriteLine($"最初のアイテム: {firstName}");
}
```

### JsonPathValueのメソッド

`JsonPathValue`は、JSONの値をラップするクラスで、以下の機能を提供します。

#### インデクサー

```csharp
// プロパティ名でアクセス
var property = value?["propertyName"];

// 配列インデックスでアクセス
var item = value?[0];
```

#### TryGetPathValue

```csharp
/// <summary>
/// ドット区切りのパスから値を取得します。
/// 配列は数値のパス要素で指定できます。
/// </summary>
bool TryGetPathValue<T>(string path, out T? result)
```

**パラメータ:**
- `path`: ドット区切りのプロパティパス（例: `"user.profile.name"`、`"items.0.id"`）
- `result`: 取得した値。見つからない場合は `default`

**戻り値:**
- 値が見つかった場合: `true`
- 値が見つからない、または型変換に失敗した場合: `false`

### 使用例

#### 例1: シンプルなJSONの読み取り

```csharp
// data.json
// {
//   "name": "Test Item",
//   "version": "1.0.0",
//   "author": "John Doe"
// }

var reader = new JsonPathReader("data.json");
var value = reader.Read();

var name = value?["name"] as string;
var version = value?["version"] as string;
var author = value?["author"] as string;

Console.WriteLine($"{name} v{version} by {author}");
```

#### 例2: ネストしたオブジェクトの読み取り

```csharp
// config.json
// {
//   "app": {
//     "name": "MyApp",
//     "settings": {
//       "theme": "dark",
//       "language": "ja"
//     }
//   }
// }

var reader = new JsonPathReader("config.json");
var value = reader.Read();

// インデクサーを使ったアクセス
var theme = value?["app"]["settings"]["theme"] as string;

// TryGetPathValueを使ったアクセス
if (value?.TryGetPathValue<string>("app.settings.language", out var lang) == true)
{
    Console.WriteLine($"言語: {lang}");
}
```

#### 例3: 配列の読み取り

```csharp
// items.json
// {
//   "items": [
//     { "id": 1, "name": "Item 1" },
//     { "id": 2, "name": "Item 2" },
//     { "id": 3, "name": "Item 3" }
//   ]
// }

var reader = new JsonPathReader("items.json");
var value = reader.Read();

// 配列の最初の要素
if (value?.TryGetPathValue<string>("items.0.name", out var firstName) == true)
{
    Console.WriteLine($"最初のアイテム: {firstName}");
}

// 配列の3番目の要素
if (value?.TryGetPathValue<int>("items.2.id", out var thirdId) == true)
{
    Console.WriteLine($"3番目のID: {thirdId}");
}
```

### エラーハンドリング

`Read()`は以下の場合に`null`を返します：

- ファイルが存在しない
- JSONの形式が不正
- ファイルの読み取りに失敗

```csharp
var reader = new JsonPathReader("nonexistent.json");
var value = reader.Read();

if (value is null)
{
    Console.WriteLine("ファイルの読み取りに失敗しました");
    return;
}

// 安全に値を取得
var name = value?["name"] as string ?? "Unknown";
```

### 内部のJsonNodeにアクセス

`JsonPathValue.Node`プロパティから、内部の`System.Text.Json.Nodes.JsonNode`に直接アクセスできます。

```csharp
var reader = new JsonPathReader("data.json");
var value = reader.Read();

// 内部のJsonNodeを取得
var node = value?.Node;

// JsonNodeとして操作
if (node is JsonObject obj)
{
    // JsonObjectとして処理
}
```

## JsonManager

`JsonManager`は、オブジェクトとJSON文字列の間でシリアライズ・デシリアライズを行う静的クラスです。

```csharp
using AvatarExplorer.Core.Services.IO;

// オブジェクトをJSON文字列にシリアライズ
var json = JsonManager.Serialize(myObject);

// JSON文字列をオブジェクトにデシリアライズ
var obj = JsonManager.Deserialize<MyClass>(json);
```

### JsonSerializerOptions

`JsonManager`は以下のオプションで設定された`JsonSerializerOptions`を使用します：

- `WriteIndented = true` - 読みやすいインデント付きJSON
- `PropertyNameCaseInsensitive = true` - プロパティ名の大文字小文字を無視

## JsonFileManager

`JsonFileManager<T>`は、型`T`のオブジェクトをJSONファイルとして読み書きするための汎用ヘルパークラスです。

```csharp
using AvatarExplorer.Core.Services.IO;

// JSONファイルから読み込み
var data = JsonFileManager<MyConfig>.Load("config.json");

// JSONファイルに保存
JsonFileManager<MyConfig>.Save(data, "config.json");
```

## 次のステップ

[01 - 基本的な概念とセットアップ](./01-getting-started.md) に戻って、AvatarExplorer.Coreの全体的な構成を確認できます。
