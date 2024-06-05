namespace BugByte;

internal record StringLiteral(string Value, int Index);

internal record GlobalContext(
    Dictionary<string, Token> Inclusions,
    Dictionary<string, StringLiteral> StringLiterals,
    Dictionary<string, StringLiteral> NullTerminatedStringLiterals
    )
{
    public GlobalContext() : this([], [], []) { }

    public Dictionary<string, Stack<PinnedStackItem>> PinnedStackItems { get; } = [];

    public Dictionary<string, int> Memory { get; } = [];

    private int pinnedStackItemsCount = 0;

    public PinnedStackItem PinStackItem(Token token, int count)
    {
        if (PinnedStackItems.TryGetValue(token.Word.Value, out var stack))
        {
            stack.Push(new(token, count, pinnedStackItemsCount));
        }
        else
        {
            stack = new Stack<PinnedStackItem>();
            stack.Push(new(token, count, pinnedStackItemsCount));
            PinnedStackItems.Add(token.Word.Value, stack);
        }

        pinnedStackItemsCount += count;
        return stack.Peek();
    }

    public void UnpinStackItem(Token token)
    {
        var name = token.Word.Value;
        if (PinnedStackItems.TryGetValue(name, out var stack))
        {
            if (stack.Count is 1)
            {
                PinnedStackItems.Remove(name);
            }
            else
            {
                PinnedStackItems[name].Pop();
            }
            pinnedStackItemsCount--;
        }
        else
        {
            throw new Exception($"Cannot unpin {name} because it is not pinned.");
        }
    }

    public void AddInclusion(Token inclusion)
    {
        if (!Inclusions.TryAdd(inclusion.Word.Value, inclusion))
        {
            throw new Exception($"Duplicate inclusion {inclusion}.");
        }
    }

    internal void AddMemory(string name, int size)
    {
        Memory.TryAdd(name, size);
    }
}
