namespace BugByte;

internal record Structure(Token Token, Dictionary<string, StructureField> Fields) : IDefinition
{
    public string Name => Token.Word.Value;
    internal int Size => Fields.Sum(f => f.Value.Size);

    public static Structure String { get; } = new(Token.OnlyValue("str"), new()
    {
        ["length"] = new StructureField(0, 8, Primitives.Number, "length"),
        ["start"] = new StructureField(8, 8, Primitives.Pointer, "start"),
    });

    public static Structure ZeroTerminatedString { get; } = new(Token.OnlyValue("0str"), new()
    {
        ["start"] = new StructureField(0, 8, Primitives.Pointer, "start"),
    });

    public Primitives[] Decompose() => Fields.Values
        .OrderBy(f => f.Offset)
        .Select(f => f.Type)
        .ToArray();
}

internal record StructureField(int Offset, int Size, Primitives Type, string Name);
