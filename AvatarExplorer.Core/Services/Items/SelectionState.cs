
namespace AvatarExplorer.Core.Services.Items;

public record SelectionNode(Guid Id, string Value);

internal class SelectionState
{
    public event Action? SelectionChanged;
    
    private readonly Stack<SelectionNode> _stack = new();

    public Guid Push(string value)
    {
        // if (state == ItemTagStates.SearchItem && FirstOrDefault(ItemTagStates.SearchItem) != null)
        // {
        //     foreach (string itemTagState in _stack)
        //     {
        //         Pop();
        //         if (itemTagState.StartsWith("searchitem")) break;
        //     }
        // }
        // TODO: 検索結果は複数無いようにする

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
    
    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _stack.Reverse();
}
