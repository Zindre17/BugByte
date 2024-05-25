namespace BugByte;

internal record Constant(Token Token, DataType Type, long? Number = null, string? Text = null, bool? Bool = null) : IAssemblable, ITypeCheckable
{
    public Contract Contract => new([], [Type]);
    public string Name => Token.Word.Value;
    public string[] Assemble() => throw new NotImplementedException();

    public void TypeCheck(TypeStack currentStack) => Contract.TypeCheck(Token, currentStack);
}
