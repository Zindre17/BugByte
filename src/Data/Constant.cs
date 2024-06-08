namespace BugByte;

internal enum ConstantTypes
{
    Number,
    String,
    ZeroTerminatedString,
}

internal record Constant(Token Token, ConstantTypes Type, long? Number = null, string? Text = null, bool? Bool = null) : IAssemblable, ITypeCheckable
{
    public Contract Contract => Type switch
    {
        ConstantTypes.Number => Contract.Producer(Primitives.Number),
        ConstantTypes.ZeroTerminatedString => Contract.Producer(Primitives.Pointer),
        ConstantTypes.String => Contract.Producer(Primitives.Number, Primitives.Pointer),
        _ => throw new NotImplementedException(),
    };
    public string Name => Token.Word.Value;
    public string[] Assemble() => throw new NotImplementedException();

    public void TypeCheck(TypeStack currentStack, Dictionary<string, Stack<Primitives>> runtimePins) => Contract.TypeCheck(Token, currentStack);
}
