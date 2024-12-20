namespace BugByte;

internal record FunctionMeta(Token Name, SourceCode Body, Contract Contract, List<ParameterType> InputPins);

internal record Function(Token Token, Contract Contract, bool AutoUsings, List<IProgramPiece> Body) : IProgramPiece, IDefinition
{
    public void Assemble(IAssemblyContext context) => Body.ForEach(i => i.Assemble(context));

    private bool MatchesContract(Dictionary<string, Stack<Primitives>> runtimePins)
    {
        var stack = new TypeStack();
        foreach (var @in in Contract.In.Decompose())
        {
            stack.Push((@in, new Token("", new Word(""), 0, 0)));
        }
        foreach (var piece in Body)
        {
            piece.TypeCheck(stack, runtimePins);
        }
        if (stack.Count != Contract.Out.Decompose().Length)
        {
            Console.WriteLine($"Did not produce expected amount of values: {stack.Count} != {Contract.Out.Length}\n{stack}");
            return false;
        }
        var outs = new Stack<TypingType>(Contract.Out.Decompose());
        while (stack.Count > 0)
        {
            var (type, _) = stack.Pop();
            var expected = outs.Pop();
            if (type != expected && !(type.IsPointer() && expected.IsPointer()))
            {
                Console.WriteLine($"Did not produce expected type: {type} != {expected}\n{stack}");
                return false;
            }
        }
        return true;
    }

    public void TypeCheck(TypeStack currentStack, Dictionary<string, Stack<Primitives>> runtimePins)
    {
        if (!MatchesContract(runtimePins))
        {
            throw new Exception($"Function {Token} does not match its contract.\n{currentStack}");
        }
        Contract.TypeCheck(Token, currentStack);
    }
}
