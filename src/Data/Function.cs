namespace BugByte;

internal record FunctionMeta(Token Name, List<Token> Body, Contract Contract, List<ParameterType> InputPins, Context Context);

internal record Function(Token Token, Contract Contract, bool AutoUsings, List<IProgramPiece> Body) : IProgramPiece
{
    public string[] Assemble() => Body.SelectMany(i => i.Assemble()).ToArray();

    private bool MatchesContract()
    {
        var stack = new TypeStack();
        foreach (var @in in Contract.In)
        {
            stack.Push((@in, new Token("", new Word(""), 0, 0)));
        }
        foreach (var piece in Body)
        {
            piece.TypeCheck(stack);
        }
        if (stack.Count != Contract.Out.Length)
        {
            Console.WriteLine($"Did not produce expected amount of values: {stack.Count} != {Contract.Out.Length}\n{stack}");
            return false;
        }
        var outs = new Stack<Primitives>(Contract.Out);
        while (stack.Count > 0)
        {
            var (type, _) = stack.Pop();
            var expected = outs.Pop();
            if (type != expected)
            {
                Console.WriteLine($"Did not produce expected type: {type} != {expected}\n{stack}");
                return false;
            }
        }
        return true;
    }

    public void TypeCheck(TypeStack currentStack)
    {
        if (!MatchesContract())
        {
            throw new Exception($"Function {Token} does not match its contract.\n{currentStack}");
        }
        Contract.TypeCheck(Token, currentStack);
    }
}
