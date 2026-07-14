using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal class SelectionState
{
    public event Action? SelectionChanged;

    private readonly Stack<string> _stack = new();

    public void Push(string value)
    {
        // if (state == ItemTagStates.SearchItem && FirstOrDefault(ItemTagStates.SearchItem) != null)
        // {
        //     foreach (string itemTagState in _stack)
        //     {
        //         Pop();
        //         if (itemTagState.StartsWith("searchitem")) break;
        //     }
        // }

        _stack.Push(value);

        SelectionChanged?.Invoke();
    }

    public string? Pop()
    {
        if (_stack.Count == 0) return null;
        var node = _stack.Pop();
        SelectionChanged?.Invoke();
        return node;
    }

    public string? Current => _stack.Count > 0 ? _stack.Peek() : null;

    public string? Root => _stack.Count > 0 ? _stack.Last() : null;

    public void Clear()
    {
        _stack.Clear();
        SelectionChanged?.Invoke();
    }

    public string? FirstOrDefault(string prefix) => _stack.FirstOrDefault(i => i.StartsWith(prefix));
    
    public IEnumerable<string> GetCurrentSelectionNodes() => _stack.Reverse();
}
