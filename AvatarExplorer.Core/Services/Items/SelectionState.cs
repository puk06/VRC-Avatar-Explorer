using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal class SelectionState
{
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
    }

    public SelectionNode? Pop()
    {
        if (_stack.Count == 0) return null;
        return _stack.Pop();
    }

    public SelectionNode? Current => _stack.Count > 0 ? _stack.Peek() : null;

    public SelectionNode? Root => _stack.Count > 0 ? _stack.Last() : null;

    public void Clear() => _stack.Clear();

    public SelectionNode? FirstOrDefault(ItemTagStates state) => _stack.FirstOrDefault(i => state.HasFlag(i.State));
    
    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _stack.Reverse();
}
