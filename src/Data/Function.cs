namespace BugByte;

internal record FunctionMeta(Token Name, List<Token> Body, Contract Contract, Context Context);

internal record Function(Token Token, Contract Contract, List<IProgramPiece> Body) : IProgramPiece
{
    public string[] Assemble() => Body.SelectMany(i => i.Assemble()).ToArray();

    private bool MatchesContract()
    {
        var stack = new TypeStack();
        foreach (var @in in Contract.In)
        {
            stack.Push((@in, new Token("", "", 0, 0)));
        }
        foreach (var piece in Body)
        {
            piece.TypeCheck(stack);
        }
        if (stack.Count != Contract.Out.Length)
        {
            return false;
        }
        var outs = new Stack<DataType>(Contract.Out);
        while (stack.Count > 0)
        {
            var (type, _) = stack.Pop();

            if (type != outs.Pop())
            {
                return false;
            }
        }
        return true;
    }

    public void TypeCheck(TypeStack currentStack)
    {
        if (!MatchesContract())
        {
            throw new Exception($"Function {Token} does not match its contract.");
        }
        Contract.TypeCheck(Token, currentStack);
    }
}
