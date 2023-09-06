namespace BugByte;

internal record StringLiteral(string Value, int Index);

internal record GlobalContext(
    Dictionary<string, Token> Inclusions,
    Dictionary<string, StringLiteral> StringLiterals,
    Dictionary<string, StringLiteral> NullTerminatedStringLiterals
    )
{
    public GlobalContext() : this(
        new(),
        new(),
        new()
        )
    { }

    public Dictionary<string, Stack<PinnedStackItem>> PinnedStackItems { get; } = new();

    public Dictionary<string, int> Memory { get; } = new();

    private int pinnedStackItemsCount = 0;

    public PinnedStackItem PinStackItem(Token token)
    {
        if (PinnedStackItems.TryGetValue(token.Value, out var stack))
        {
            stack.Push(new(token, pinnedStackItemsCount));
        }
        else
        {
            stack = new Stack<PinnedStackItem>();
            stack.Push(new(token, pinnedStackItemsCount));
            PinnedStackItems.Add(token.Value, stack);
        }

        pinnedStackItemsCount++;
        return stack.Peek();
    }

    public void UnpinStackItem(string name)
    {
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
        if (!Inclusions.TryAdd(inclusion.Value, inclusion))
        {
            throw new Exception($"Duplicate inclusion {inclusion}.");
        }
    }

    internal void AddMemory(string name, int size)
    {
        Memory.TryAdd(name, size);
    }
}
