namespace BugByte;

internal static class MemoryAllocation
{
    public static MemoryAllocationType None => new(Token.OnlyValue(string.Empty), Typing.Create(Primitives.Unknown), string.Empty, 0);

    public static MemoryAllocationType Create(Token label, string scope, TypingType typing, int count) => new(label, typing, GenerateMemoryLabel(scope, label.Word.Value), count);

    public static string GetAssemblyLabel(this MemoryAllocationType memory) => memory.AssemblyLabel;
    public static int GetSize(this MemoryAllocationType memory) => memory.Typing.GetSize() * memory.Count;


    private static string GenerateMemoryLabel(string contextName, string memoryLabel)
    {
        return $"{contextName.Replace('-', '_')}_{memoryLabel.Replace('-', '_')}";
    }
}

internal record MemoryAllocationType(Token Token, TypingType Typing, string AssemblyLabel, int Count);
