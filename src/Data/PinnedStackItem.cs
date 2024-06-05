namespace BugByte;

internal record PinnedStackItem(Token Token, int ItemCount, int Index)
{
    public Primitives[] Types { get; set; } = Enumerable.Repeat(Primitives.Unknown, ItemCount).ToArray();
}
