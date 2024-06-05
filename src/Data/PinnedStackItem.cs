namespace BugByte;

internal record PinnedStackItem(Token Token, int Index)
{
    public Primitives Type { get; set; } = Primitives.Unknown;
}
