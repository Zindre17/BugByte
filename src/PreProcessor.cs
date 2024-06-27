namespace BugByte;

internal record PreProcesesingResult(SourceCode RemainingCode, Definitions Definitions);

internal class PreProcessor
{
    public static PreProcesesingResult Process(SourceCode code)
    {
        var (definitions, remainingCode) = FindDefinitions(code);
        return new(remainingCode, definitions);
    }

    private static (Definitions, SourceCode) FindDefinitions(SourceCode code)
    {
        var definitions = new Definitions();
        var remainingCode = new SourceCodeBuilder();
        while (code.HasNextToken())
        {
            var token = code.MoveNext();
            if (token.Word.Value is Tokens.Keyword.Struct)
            {
                var structName = code.MoveNext();
                code.MoveNext();
                var structCode = FindCodeBlock(code);
                definitions.Add(new StructDefinition(structName, structCode));
            }
            else if (token.Word.Value is Tokens.Keyword.ConstantDefinition)
            {
                if (!code.HasRemainingTokens(2))
                {
                    throw new Exception($"Expected at least two tokens after `aka`, but got nothing @ {code.CurrentToken()}");
                }
                var nameToken = code.MoveNext();
                var constant = code.MoveNext();

                definitions.Add(new ConstantDefinition(nameToken, new([constant])));
            }
            else if (IsFunction(code))
            {
                var functionName = token;
                var functionParameters = FindFunctionParameters(code);
                var functionOutput = FindFunctionOutput(code);
                var functionCode = FindCodeBlock(code);
                definitions.Add(new FunctionDefinition(functionName, functionParameters, functionOutput, functionCode));
            }
            else if (token.Word.Value is Tokens.Keyword.Include)
            {
                var includePath = code.MoveNext();
                var importCode = Lexer.LexFile(includePath.Word.Value);
                var (importDefinitions, _) = FindDefinitions(importCode);
                definitions.Include(importDefinitions);
            }
            //TODO handle global memory allocation
            else
            {
                remainingCode.Add(token);
            }
        }
        return (definitions, remainingCode.Build());
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

    private static SourceCode FindCodeBlock(SourceCode code)
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

internal interface IDefinition
{
    Token Name { get; }
    SourceCode Code { get; }
}

internal record struct StructDefinition(Token Name, SourceCode Code) : IDefinition;
internal record struct ConstantDefinition(Token Name, SourceCode Code) : IDefinition;
internal record struct FunctionDefinition(Token Name, SourceCode Parameters, SourceCode Output, SourceCode Body) : IDefinition
{
    public readonly SourceCode Code => Body;

}

internal class Definitions
{
    private readonly Dictionary<string, IDefinition> definitions = [];

    public int Count => definitions.Count;

    public void Add(IDefinition definition)
    {
        if (definitions.TryGetValue(definition.Name.Word.Value, out var existingDefinition))
        {
            throw new AlreadyDefinedException(existingDefinition.Name, definition.Name);
        }
        definitions.Add(definition.Name.Word.Value, definition);
    }

    public void Include(Definitions importDefinitions)
    {
        foreach (var (_, definition) in importDefinitions.definitions)
        {
            Add(definition);
        }
    }

    public IDefinition Get(string name)
    {
        if (!definitions.TryGetValue(name, out var definition))
        {
            throw new NotDefinedException(name);
        }
        return definition;
    }
}

internal class NotDefinedException(string name)
    : Exception($"{name} is not defined.")
{
}

internal class AlreadyDefinedException(Token existingDefinition, Token newDefinition)
    : Exception($"{newDefinition.Word.Value} is already defined from {existingDefinition} @ {newDefinition}")
{
}
