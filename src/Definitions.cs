namespace BugByte;

internal class Definitions(string scope, Definitions? parent = null)
{
    private readonly Definitions? parent = parent;
    private readonly Dictionary<string, FunctionDefinition> functionDefinitions = [];
    private readonly Dictionary<string, StructDefinition> structDefinitions = [];
    private readonly Dictionary<string, ConstantDefinition> constantDefinitions = [];
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

    public void AddMemory(Token nameToken, TypingType typing, int count)
    {
        var name = nameToken.Word.Value;
        if (memoryAllocations.TryGetValue(name, out var existingMemoryAllocation))
        {
            throw new AlreadyDefinedException(existingMemoryAllocation.Token, nameToken);
        }
        memoryAllocations.Add(name, MemoryAllocation.Create(nameToken, scope, typing, count));
    }

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


internal interface IIdentifiedDefinition<TDefinition> where TDefinition : class, IDefinition
{
    Token Token { get; }

    TDefinition Parse(IScope context);
}

internal interface IDefinition;

internal record StructDefinition(Token Token, SourceCode Code) : IIdentifiedDefinition<Structure>
{
    public Structure Parse(IScope context) => ParseInternal(context);

    private Structure ParseInternal(IScope context)
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
    public Constant Parse(IScope context) => ParseInternal(context);
    private Constant ParseInternal(IScope context)
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
        else if (constant.Word is ZeroTerminatedStringLiteralWord constZeroString)
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
    public Function Parse(IScope context)
    {
        var functionInput = Parser.ParseParameters(context, Parameters);
        Parameters.Reset();

        var inputPins = functionInput.All(p => p.IsNamed()) ? functionInput : [];

        List<IProgramPiece> funcProgram = [];
        var pinnedInputItems = inputPins.Reverse<ParameterType>()
            .Select(p => context.Pin(p.GetNameToken(), p.Typing))
            .ToList();

        pinnedInputItems.ForEach(item => funcProgram.Add(Instructions.PinStackItem(item.GetPinInfo())));
        var (remainingInnerCode, innerDefinitions) = PreProcessor.Process(Body, Token.Word.Value, context);
        Body.Reset();

        funcProgram.AddRange(Parser.ParseProgram(remainingInnerCode, innerDefinitions));

        var functionOutput = Parser.ParseParameters(context, Output);
        Output.Reset();
        var contract = new Contract(functionInput.SelectMany(p => p.Typing.Decompose()).ToArray(), functionOutput.SelectMany(p => p.Typing.Decompose()).ToArray());
        var parsedFunc = new Function(Token, contract, inputPins.Count is not 0, funcProgram);

        pinnedInputItems.Reverse<IScopedPin>().ForEach(item => item.Unpin());
        return parsedFunc;
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
