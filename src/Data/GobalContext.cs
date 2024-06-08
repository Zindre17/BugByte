namespace BugByte;

internal record StringLiteral(string Value, int Index);

internal record GlobalContext(
    Dictionary<string, Token> Inclusions,
    Dictionary<string, StringLiteral> StringLiterals,
    Dictionary<string, StringLiteral> NullTerminatedStringLiterals
    )
{
    public GlobalContext() : this([], [], []) { }

    public Dictionary<string, Stack<PinnedStackItemType>> PinnedStackItems { get; } = [];

    public Dictionary<string, int> Memory { get; } = [];

    private int pinnedStackItemsCount = 0;

    public PinnedStackItemType PinStackItem(Token token, TypingType typing)
    {
        if (!PinnedStackItems.TryGetValue(token.Word.Value, out var stack))
        {
            stack = new Stack<PinnedStackItemType>();
            PinnedStackItems.Add(token.Word.Value, stack);
        }

        stack.Push(PinnedStackItem.Create(token, typing, pinnedStackItemsCount));
        pinnedStackItemsCount += typing.ToPrimitives().Length;
        return stack.Peek();
    }

    public void UnpinStackItem(Token token)
    {
        var name = token.Word.Value;
        if (PinnedStackItems.TryGetValue(name, out var stack))
        {
            var item = stack.Pop();
            if (stack.Count is 0)
            {
                PinnedStackItems.Remove(name);
            }

            pinnedStackItemsCount -= item.Typing.ToPrimitives().Length;
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

    internal void AddMemory(MemoryAllocationType memoryAllocationType)
    {
        Memory.TryAdd(memoryAllocationType.GetAssemblyLabel(), memoryAllocationType.GetSize());
    }
}
