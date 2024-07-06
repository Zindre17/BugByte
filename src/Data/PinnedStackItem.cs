namespace BugByte;

interface IPinnedStackItem
{
    Token Token { get; }
    TypingType Typing { get; }
    int Offset { get; }
};

internal record struct PinnedStackItemType(Token Token, TypingType Typing, int Offset) : IPinnedStackItem;

public static class PinnedStackItem
{
    static int offset = 0;

    internal static IPinnedStackItem Create(Token token, TypingType typing)
    {
        var pinnedItem = new PinnedStackItemType(token, typing, offset);
        offset += typing.ToPrimitives().Length;
        return pinnedItem;
    }

    internal static void Unpin(this IPinnedStackItem item)
    {
        if (item is not PinnedStackItemType)
        {
            throw new ArgumentException("Unknown IPinnedStackItem type.");
        }
        if (offset != item.Offset + item.Typing.ToPrimitives().Length)
        {
            throw new Exception($"Cannot unpin {item.Token} because it is not the top pinned item.");
        }
        offset -= item.Typing.ToPrimitives().Length;
    }
}
