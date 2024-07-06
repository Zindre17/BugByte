namespace BugByte;

interface IScope
{
    Definitions Definitions { get; }
};
internal record struct GlobalScope(Definitions Definitions) : IScope;
internal record struct NestedScope(string Name, Definitions Definitions, IScope Parent) : IScope;

internal static class Scope
{
    public static IScope Create() => new GlobalScope();
    public static string GetScopeName(this IScope scope) => scope switch
    {
        GlobalScope => "global",
        NestedScope nested => $"{GetScopeName(nested.Parent)}_{nested.Name}",
        _ => throw new Exception("Unknown scope type."),
    };
}

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

internal interface IIdentifiedDefinition<TDefinition> where TDefinition : class, IDefinition
{
    Token Token { get; }

    TDefinition Parse(Definitions context);
}

internal interface IDefinition;

internal record StructDefinition(Token Token, SourceCode Code) : IIdentifiedDefinition<Structure>
{
    public Structure Parse(Definitions context) => ParseInternal(context);

    private Structure ParseInternal(Definitions context)
    {
        var fields = new Dictionary<string, StructureField>();
        var offset = 0;
        while (Code.HasNextToken())
        {
            var member = Code.MoveNext();
            if (member.Word.Value is ";")
            {
                throw new Exception($"Expected identifier, but got nothing after {member}");
            }

            var sizeToken = Code.MoveNext();
            if (sizeToken.Word.Value is ";")
            {
                break;
            }

            var type = Primitives.Unknown;
            if (int.TryParse(sizeToken.Word.Value, out var size))
            {
            }
            else if (Parser.TryParseTyping(context, sizeToken, out var typing))
            {
                size = typing.GetSize();
                type = typing.ToPrimitives().First();
            }
            else
            {
                throw new Exception($"Expected integer, but got {sizeToken}.");
            }

            fields.Add(member.Word.Value, new(offset, size, type, member.Word.Value));
            offset += size;
        }
        Code.Reset();
        return new Structure(Token, fields);
    }

}
internal record ConstantDefinition(Token Token, SourceCode Code) : IIdentifiedDefinition<Constant>
{
    public Constant Parse(Definitions context) => ParseInternal(context);
    private Constant ParseInternal(Definitions context)
    {
        if (!Code.HasNextToken())
        {
            throw new Exception($"Expected at least one token as value for constant, but got nothing @ {Token}");
        }

        var constant = Code.MoveNext();
        Code.Reset();
        if (int.TryParse(constant.Word.Value, out var constInt))
        {
            return new(Token, ConstantTypes.Number, Number: constInt);
        }
        else if (constant.Word is StringLiteralWord constString)
        {
            return new(Token, ConstantTypes.String, Text: constString.Value);
        }
        else if (constant.Word is NullTerminatedStringLiteralWord constZeroString)
        {
            return new(Token, ConstantTypes.ZeroTerminatedString, Text: constZeroString.Value);
        }
        else
        {
            throw new Exception($"Expected number/string after `aka`, but got {constant} @ {constant.Filename}:{constant.Line}:{constant.Column}");
        }
    }
}
internal record FunctionDefinition(Token Token, SourceCode Parameters, SourceCode Output, SourceCode Body) : IIdentifiedDefinition<Function>
{
    public SourceCode Code => Body;

    public Function Parse(Definitions context)
    {
        var functionInput = Parser.ParseParameters(context, Parameters);
        Parameters.Reset();

        var inputPins = functionInput.All(p => p.IsNamed()) ? functionInput : [];

        List<IProgramPiece> funcProgram = [];
        var pinnedInputItems = inputPins.Reverse<ParameterType>()
            .Select(p => context.PinStackItem(p.GetNameToken(), p.Typing))
            .ToList();

        pinnedInputItems.ForEach(item => funcProgram.Add(Instructions.PinStackItem(item.Current)));
        var (remainingInnerCode, innerDefinitions) = PreProcessor.Process(Body, Token.Word.Value, context);
        Body.Reset();

        funcProgram.AddRange(Parser.ParseProgram(remainingInnerCode, innerDefinitions));

        var functionOutput = Parser.ParseParameters(context, Output);
        Output.Reset();
        var contract = new Contract(functionInput.SelectMany(p => p.Typing.Decompose()).ToArray(), functionOutput.SelectMany(p => p.Typing.Decompose()).ToArray());
        var parsedFunc = new Function(Token, contract, inputPins.Count is not 0, funcProgram);

        pinnedInputItems.Reverse<NamedPin>().ForEach(item => item.Unpin());
        return parsedFunc;
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


internal class Definitions(string scope, Definitions? parent = null)
{
    private readonly Definitions? parent = parent;
    private readonly Dictionary<string, FunctionDefinition> functionDefinitions = [];
    private readonly Dictionary<string, StructDefinition> structDefinitions = [];
    private readonly Dictionary<string, ConstantDefinition> constantDefinitions = [];
    private readonly Dictionary<string, NamedPin> pinnedStackItems = [];
    private readonly Dictionary<string, MemoryAllocationType> memoryAllocations = [];

    private readonly string scope = parent is not null ? $"{parent.scope}_{scope}" : scope;

    public int Count => functionDefinitions.Count + structDefinitions.Count + constantDefinitions.Count;

    public void AddFunction(FunctionDefinition functionDefinition)
    {
        if (functionDefinitions.TryGetValue(functionDefinition.Token.Word.Value, out FunctionDefinition? value))
        {
            throw new AlreadyDefinedException(value.Token, functionDefinition.Token);
        }
        functionDefinitions.Add(functionDefinition.Token.Word.Value, functionDefinition);
    }

    public void AddConstant(ConstantDefinition constant)
    {
        if (constantDefinitions.TryGetValue(constant.Token.Word.Value, out ConstantDefinition? value))
        {
            throw new AlreadyDefinedException(value.Token, constant.Token);
        }
        constantDefinitions.Add(constant.Token.Word.Value, constant);
    }

    public void AddStructure(StructDefinition structDefinition)
    {
        if (structDefinitions.TryGetValue(structDefinition.Token.Word.Value, out StructDefinition? value))
        {
            throw new AlreadyDefinedException(value.Token, structDefinition.Token);
        }
        structDefinitions.Add(structDefinition.Token.Word.Value, structDefinition);
    }

    public void Include(Definitions importDefinitions)
    {
        foreach (var (_, definition) in importDefinitions.functionDefinitions)
        {
            AddFunction(definition);
        }
        foreach (var (_, definition) in importDefinitions.structDefinitions)
        {
            AddStructure(definition);
        }
        foreach (var (_, definition) in importDefinitions.constantDefinitions)
        {
            AddConstant(definition);
        }
    }

    public NamedPin PinStackItem(Token token, TypingType typing)
    {
        if (!TryGetPin(token, out var namedPin) && namedPin is null)
        {
            namedPin = new NamedPin();
            pinnedStackItems.Add(token.Word.Value, namedPin);
        }

        namedPin.Pin(token, typing);

        return namedPin;
    }

    public bool TryGetPin(Token token, out NamedPin namedPin) => TryGetPin(token.Word.Value, out namedPin);
    public bool TryGetPin(string name, out NamedPin namedPin)
    {
        if (pinnedStackItems.TryGetValue(name, out namedPin!))
        {
            return namedPin.HasAny();
        }
        if (parent is not null)
        {
            return parent.TryGetPin(name, out namedPin);
        }
        return false;
    }

    public void AddMemory(Token nameToken, TypingType typing, int count)
    {
        var name = nameToken.Word.Value;
        if (memoryAllocations.TryGetValue(name, out var existingMemoryAllocation))
        {
            throw new AlreadyDefinedException(existingMemoryAllocation.Token, nameToken);
        }
        memoryAllocations.Add(name, MemoryAllocation.Create(nameToken, scope, typing, count));
    }

    public bool TryGetConstant(Token name, out IIdentifiedDefinition<Constant> constant)
        => TryGetConstant(name.Word.Value, out constant);
    public bool TryGetConstant(string name, out IIdentifiedDefinition<Constant> constant)
    {
        if (constantDefinitions.TryGetValue(name, out var definition) && definition is ConstantDefinition constantDefinition)
        {
            constant = constantDefinition;
            return true;
        }
        if (parent is not null)
        {
            return parent.TryGetConstant(name, out constant);
        }
        constant = null!;
        return false;
    }

    public bool TryGetStructure(Token nameToken, out IIdentifiedDefinition<Structure> structure) => TryGetStructure(nameToken.Word.Value, out structure);
    public bool TryGetStructure(string name, out IIdentifiedDefinition<Structure> structure)
    {
        if (structDefinitions.TryGetValue(name, out var definition))
        {
            structure = definition;
            return true;
        }
        if (parent is not null)
        {
            return parent.TryGetStructure(name, out structure);
        }
        structure = null!;
        return false;
    }

    internal bool TryGetMemory(string value, out MemoryAllocationType memoryAllocation)
    {
        if (memoryAllocations.TryGetValue(value, out memoryAllocation!))
        {
            return true;
        }
        if (parent is not null)
        {
            return parent.TryGetMemory(value, out memoryAllocation);
        }
        return false;
    }

    internal bool TryGetFunction(string value, out IIdentifiedDefinition<Function> func)
    {
        if (functionDefinitions.TryGetValue(value, out var definition) && definition is FunctionDefinition functionDefinition)
        {
            func = functionDefinition;
            return true;
        }
        if (parent is not null)
        {
            return parent.TryGetFunction(value, out func);
        }
        func = null!;
        return false;
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
