namespace BugByte;

internal record PinnedStackItemType(Token Token, TypingType Typing, int Index);

public static class PinnedStackItem
{
    internal static PinnedStackItemType Create(Token token, TypingType typing, int pinnedStackItemsCount) => new(token, typing, pinnedStackItemsCount);
}
