using System.Text.Json;
using System.Text.Json.Nodes;

namespace AvatarExplorer.Core.Services.IO;

/// <summary>
/// JSON ファイルを読み込み、プロパティを簡単に辿れる値として公開します。
/// </summary>
public sealed class JsonPathReader(string path)
{
    /// <summary>このリーダーに関連付けられたパスを取得します。</summary>
    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

    /// <summary>
    /// <see cref="Path"/> の JSON ファイルを読み込みます。
    /// </summary>
    /// <returns>読み込んだ値。不正な JSON の場合は null。</returns>
    public JsonPathValue? Read()
    {
        try
        {
            var node = JsonNode.Parse(File.ReadAllText(Path));
            return node is null ? null : new JsonPathValue(node);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// JSON の値をプロパティパスで読み取るための薄いラッパーです。
/// </summary>
public sealed class JsonPathValue
{
    private readonly JsonNode _node;

    internal JsonPathValue(JsonNode node) => _node = node;

    /// <summary>内部の <see cref="JsonNode"/> を取得します。</summary>
    public JsonNode Node => _node;

    /// <summary>
    /// JSON オブジェクトのプロパティを取得します。値がプリミティブの場合は CLR 値を返します。
    /// </summary>
    /// <param name="propertyName">取得するプロパティ名。</param>
    /// <returns>プロパティ値、または存在しない場合は null。</returns>
    public dynamic? this[string propertyName]
    {
        get
        {
            if (_node is not JsonObject jsonObject || !jsonObject.TryGetPropertyValue(propertyName, out var value))
            {
                return null;
            }

            return Wrap(value);
        }
    }

    /// <summary>
    /// JSON 配列の要素を取得します。
    /// </summary>
    /// <param name="index">取得する配列インデックス。</param>
    /// <returns>配列要素、または範囲外の場合は null。</returns>
    public dynamic? this[int index]
    {
        get
        {
            if (_node is not JsonArray jsonArray || index < 0 || index >= jsonArray.Count)
            {
                return null;
            }

            return Wrap(jsonArray[index]);
        }
    }

    /// <summary>
    /// ドット区切りのプロパティパスから値を取得します。
    /// 配列は数値のパス要素（例: <c>items.0.name</c>）で指定できます。
    /// </summary>
    public bool TryGetPathValue<T>(string path, out T? result)
    {
        ArgumentNullException.ThrowIfNull(path);

        JsonNode? current = _node;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current switch
            {
                JsonObject obj when obj.TryGetPropertyValue(part, out var value) => value,
                JsonArray array when int.TryParse(part, out var index) && index >= 0 && index < array.Count => array[index],
                _ => null
            };

            if (current is null)
            {
                result = default;
                return false;
            }
        }

        try
        {
            result = current.Deserialize<T>(JsonManager.JsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            result = default;
            return false;
        }
    }

    private static object? Wrap(JsonNode? node)
    {
        if (node is JsonObject or JsonArray)
        {
            return new JsonPathValue(node);
        }

        if (node is not JsonValue value)
        {
            return null;
        }

        using var document = JsonDocument.Parse(value.ToJsonString());
        var element = document.RootElement;
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}
