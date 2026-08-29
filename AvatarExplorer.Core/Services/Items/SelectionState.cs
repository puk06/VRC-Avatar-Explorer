namespace AvatarExplorer.Core.Services.Items;

/// <summary>
/// ナビゲーションの選択履歴において1つの選択状態（ノード）を表すレコードです。
/// </summary>
/// <param name="Id">ノードを一意に識別するID。</param>
/// <param name="Value">選択されたIdentifier文字列。</param>
public record SelectionNode(Guid Id, string Value);

internal class SelectionState
{
    public event Action? SelectionChanged;

    private readonly Stack<SelectionNode> _stack = new();

    public Guid Push(string value)
    {
        var newNode = new SelectionNode(Guid.NewGuid(), value);
        _stack.Push(newNode);
        SelectionChanged?.Invoke();

        return newNode.Id;
    }

    public SelectionNode? Pop()
    {
        if (_stack.Count == 0) return null;
        var node = _stack.Pop();
        SelectionChanged?.Invoke();
        return node;
    }

    public SelectionNode? Current => _stack.Count > 0 ? _stack.Peek() : null;
    public SelectionNode? Root => _stack.Count > 0 ? _stack.Last() : null;

    public void Clear()
    {
        _stack.Clear();
        SelectionChanged?.Invoke();
    }

    public SelectionNode? FirstOrDefault(string prefix) => _stack.FirstOrDefault(i => i.Value.StartsWith(prefix));
    public SelectionNode? LastOrDefault(string prefix) => _stack.LastOrDefault(i => i.Value.StartsWith(prefix));

    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _stack.Reverse();
}
