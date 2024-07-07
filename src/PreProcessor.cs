namespace BugByte;

internal record PreProcesesingResult(SourceCode RemainingCode, Definitions Definitions);

internal class PreProcessor
{
    public static PreProcesesingResult Process(SourceCode code, string newScope, Definitions parent = null!)
    {
        var definitions = new Definitions(newScope, parent);
        var (_, remainingCode) = FindDefinitions(definitions, code);
        return new(remainingCode, definitions);
    }

    private static (Definitions, SourceCode) FindDefinitions(Definitions definitions, SourceCode code)
    {
        var remainingCode = new SourceCodeBuilder();
        while (code.HasNextToken())
        {
            var token = code.MoveNext();
            if (token.Word.Value is Tokens.Keyword.Struct)
            {
                definitions.AddStructure(DefineStruct(code));
            }
            else if (token.Word.Value is Tokens.Keyword.ConstantDefinition)
            {
                definitions.AddConstant(DefineConstant(code));
            }
            else if (IsFunction(code))
            {
                definitions.AddFunction(DefineFunction(code));
            }
            else if (token.Word.Value is Tokens.Keyword.Include)
            {
                definitions.Include(ProcessInclude(code));
            }
            //TODO handle global memory allocation
            else
            {
                remainingCode.Add(token);
            }
        }
        return (definitions, remainingCode.Build());
    }

    private static StructDefinition DefineStruct(SourceCode code)
    {
        var structName = code.MoveNext();
        code.MoveNext();
        var structCode = FindCodeBlock(code);
        return new StructDefinition(structName, structCode);
    }

    private static ConstantDefinition DefineConstant(SourceCode code)
    {
        if (!code.HasRemainingTokens(2))
        {
            throw new Exception($"Expected at least two tokens after `aka`, but got nothing @ {code.CurrentToken()}");
        }
        var nameToken = code.MoveNext();
        var constant = code.MoveNext();

        return new ConstantDefinition(nameToken, new([constant]));
    }

    private static FunctionDefinition DefineFunction(SourceCode code)
    {
        var functionName = code.CurrentToken();
        code.MoveNext();
        var functionParameters = FindFunctionParameters(code);
        var functionOutput = FindFunctionOutput(code);
        var functionCode = FindCodeBlock(code);
        return new FunctionDefinition(functionName, functionParameters, functionOutput, functionCode);
    }

    private static Definitions ProcessInclude(SourceCode code)
    {
        var includePath = code.MoveNext();
        if (includePath.Word is not StringLiteralWord stringLiteralWord)
        {
            throw new Exception($"Expected path after include, but got {includePath}");
        }
        var path = stringLiteralWord.InnerValue;
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new Exception($"Cannot include {path}({includePath}) because it does not exist.");
        }
        Console.WriteLine($"Including {path}");
        var importCode = Lexer.LexFile(fullPath);
        var definitions = new Definitions(Path.GetFileNameWithoutExtension(path));
        var (importDefinitions, _) = FindDefinitions(definitions, importCode);
        return importDefinitions;
    }

    private static bool IsFunction(SourceCode code)
    {
        const int minumumFunctionTokens = 4;
        if (!code.HasRemainingTokens(minumumFunctionTokens))
        {
            return false;
        }
        var next = code.PeekNextToken();
        return next.Word.Value is Tokens.Operator.FunctionParametersStart;
    }

    private static SourceCode FindFunctionParameters(SourceCode code)
        => code.GetCodeUntil(Tokens.Operator.FunctionParametersEnd);

    private static SourceCode FindFunctionOutput(SourceCode code)
        => code.GetCodeUntil(Tokens.BlockStart);

    public static SourceCode FindCodeBlock(SourceCode code)
    {
        if (code.CurrentToken().Word.Value != Tokens.BlockStart)
        {
            throw new Exception($"Expected block start token @ {code.CurrentToken()}");
        }
        if (!code.HasNextToken())
        {
            throw new Exception($"Expected at least 1 token after block start; block end after {code.CurrentToken()}");
        }

        var blockDepth = 1;
        var codeBlock = code.GetCodeUntil(token =>
        {
            if (token.Word.Value == Tokens.BlockStart)
            {
                blockDepth++;
            }
            else if (token.Word.Value == Tokens.BlockEnd)
            {
                blockDepth--;
            }
            return blockDepth is 0;
        });

        if (blockDepth > 0)
        {
            throw new Exception($"Block not closed properly @ {code.CurrentToken()}");
        }

        return codeBlock;
    }
}

internal class NamedPin
{
    private readonly Stack<IPinnedStackItem> stackOfPins = [];

    public void Pin(Token token, TypingType typing)
    {
        var pin = PinnedStackItem.Create(token, typing);
        stackOfPins.Push(pin);
    }

    public void Unpin()
    {
        if (stackOfPins.Count is 0)
        {
            throw new Exception($"Cannot unpin because it is not pinned.");
        }
        var item = stackOfPins.Pop();
        item.Unpin();
    }

    public IPinnedStackItem Current => stackOfPins.Peek();
    public bool HasAny() => stackOfPins.Count > 0;
}
