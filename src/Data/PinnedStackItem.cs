namespace BugByte;

internal record PinnedStackItem(Token Token, int Index)
{
    public DataType Type { get; set; } = DataType.Unknown;
}
