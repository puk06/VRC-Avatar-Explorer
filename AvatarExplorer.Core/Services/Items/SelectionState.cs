using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal class SelectionState
{
    public event Action? SelectionChanged;

    private readonly Stack<SelectionNode> _stack = new();

    public void Push(ItemTagStates state, string key)
    {
        if (state == ItemTagStates.SearchItem && FirstOrDefault(ItemTagStates.SearchItem) != null)
        {
            foreach (ItemTagStates itemTagState in _stack.Select(i => i.State).ToArray())
            {
                Pop();
                if (itemTagState == ItemTagStates.SearchItem) break;
            }
        }

        _stack.Push(new SelectionNode(state, key));

        SelectionChanged?.Invoke();
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

    public SelectionNode? FirstOrDefault(ItemTagStates state) => _stack.FirstOrDefault(i => state.HasFlag(i.State));
    
    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _stack.Reverse();
}
