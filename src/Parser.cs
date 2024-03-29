using static BugByte.Lexer;

namespace BugByte;

internal static class Parser
{
    private static int stringLiteralCounter = 0;
    private static int nullTerminatedStringLiteralCounter = 0;
    internal static Context MetaEvaluate(
        IEnumerable<Token> tokens,
        GlobalContext meta,
        Context outerContext,
        string? terminatingString,
        out IEnumerable<Token> innerRemainingTokens,
        out IEnumerable<Token> remainingTokens
        )
    {
        remainingTokens = new Queue<Token>();
        innerRemainingTokens = new Queue<Token>();

        var nestedLevel = 0;

        var tokenQueue = new Queue<Token>(tokens);

        var innerContext = new Context(outerContext);

        while (tokenQueue.Count > 0 && (tokenQueue.Peek().Word.Value != terminatingString || nestedLevel > 0))
        {
            var token = tokenQueue.Dequeue();

            if (token.Word.Value is ":")
            {
                nestedLevel += 1;
                ((Queue<Token>)innerRemainingTokens).Enqueue(token);
            }
            else if (token.Word.Value is ";")
            {
                nestedLevel -= 1;
                ((Queue<Token>)innerRemainingTokens).Enqueue(token);
            }
            else if (token.Word.Value is Tokens.Keyword.Include)
            {
                var fileRootContext = ParseInclude(token, tokenQueue, meta);
                innerContext.Merge(fileRootContext);
            }
            else if (token.Word.Value is Tokens.Keyword.Struct)
            {
                innerContext.AddStructure(ParseStructure(token, tokenQueue, innerContext));
            }
            else if (token.Word.Value is Tokens.Keyword.ConstantDefinition)
            {
                innerContext.AddConstant(ParseConstant(token, tokenQueue, innerContext));
            }
            else if (tokenQueue.Count > 0 && tokenQueue.Peek().Word.Value is "(")
            {
                var functionName = token.Word.Value;
                tokenQueue.Dequeue();
                if (innerContext.IsReserved(functionName))
                {
                    throw new Exception($"Cannot use reserved keyword {token} as a function name.");
                }

                var arguments = ParseDataTypes(tokenQueue, ")");
                var argumentsEndToken = tokenQueue.Dequeue();
                if (argumentsEndToken.Word.Value is not ")")
                {
                    throw new Exception($"Expected `)` after function arguments, but got {argumentsEndToken}.");
                }

                var output = ParseDataTypes(tokenQueue, ":");
                var outputEndToken = tokenQueue.Dequeue();
                if (outputEndToken.Word.Value is not ":")
                {
                    throw new Exception($"Expected `:` after function output, but got {outputEndToken}.");
                }

                var contract = new Contract(arguments.ToArray(), output.ToArray());
                var functionContext = MetaEvaluate(tokenQueue, meta, innerContext, ";", out var innerRemaining, out var remaining);
                functionContext.Name = functionName;
                tokenQueue = (Queue<Token>)remaining;
                var endToken = tokenQueue.Dequeue();
                if (endToken.Word.Value is not ";")
                {
                    throw new Exception($"Expected `;` after function, but got {endToken}.");
                }
                var functionMeta = new FunctionMeta(token, innerRemaining.ToList(), contract, functionContext);
                innerContext.AddFunction(functionMeta);
            }
            else
            {
                ((Queue<Token>)innerRemainingTokens).Enqueue(token);
            }
        }
        foreach (var rem in tokenQueue)
        {
            ((Queue<Token>)remainingTokens).Enqueue(rem);
        }
        return innerContext;
    }

    public static List<IProgramPiece> ParseProgram(Queue<Token> tokens, GlobalContext meta, Context context, string? terminationToken = null)
    {
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected tokens, but got nothing.");
        }

        var programPieces = new List<IProgramPiece>();

        while (tokens.Count > 0 && tokens.Peek().Word.Value != terminationToken)
        {
            var token = tokens.Dequeue();
            if (int.TryParse(token.Word.Value, out var number))
            {
                programPieces.Add(Instructions.Literal.Number(token, number));
            }
            else if (token.Word is StringLiteralWord stringLiteralWord)
            {
                if (!meta.StringLiterals.TryGetValue(token.Word.Value, out var str))
                {
                    str = new(stringLiteralWord.InnerValue, stringLiteralCounter++);
                    meta.StringLiterals.Add(token.Word.Value, str);
                }
                programPieces.Add(Instructions.Literal.String(token, str));
            }
            else if (token.Word is NullTerminatedStringLiteralWord nullTerminatedStringLiteralWord)
            {
                if (!meta.NullTerminatedStringLiterals.TryGetValue(token.Word.Value, out var nullStr))
                {
                    nullStr = new(nullTerminatedStringLiteralWord.InnerValue, nullTerminatedStringLiteralCounter++);
                    meta.NullTerminatedStringLiterals.Add(token.Word.Value, nullStr);
                }
                programPieces.Add(Instructions.Literal.NullTerminatedString(token, nullStr.Index));
            }
            else if (token.Word.Value is Tokens.Keyword.Yes)
            {
                programPieces.Add(Instructions.Boolean.Yes(token));
            }
            else if (token.Word.Value is Tokens.Keyword.No)
            {
                programPieces.Add(Instructions.Boolean.No(token));
            }
            else if (context.TryGetFunction(token.Word.Value, out var func))
            {
                var funcProgram = ParseProgram(new Queue<Token>(func.Body), meta, func.Context, ";");
                var parsedFunc = new Function(token, func.Contract, funcProgram);
                programPieces.Add(parsedFunc);
            }
            else if (meta.PinnedStackItems.TryGetValue(token.Word.Value, out var pinnedStackItems))
            {
                programPieces.Add(Instructions.PushPinnedStackItem(pinnedStackItems.Peek()));
            }
            else if (context.TryGetConstant(token.Word.Value, out var constant))
            {
                if (constant.Type is DataType.String)
                {
                    var str = meta.StringLiterals[constant.Text!];
                    programPieces.Add(Instructions.Literal.String(token, str));
                }
                else if (constant.Type is DataType.ZeroTerminatedString)
                {
                    var zstr = meta.NullTerminatedStringLiterals[constant.Text!];
                    programPieces.Add(Instructions.Literal.NullTerminatedString(token, zstr.Index));
                }
                else if (constant.Type is DataType.Number)
                {
                    programPieces.Add(Instructions.Literal.Number(token, constant.Number!.Value));
                }
                else
                {
                    throw new Exception($"Unknown constant type {constant.Type}.");
                }
            }
            else if (token.Word.Value is Tokens.Operator.Add)
            {
                programPieces.Add(Instructions.Operations.Add(token));
            }
            else if (token.Word.Value is Tokens.Operator.Subtract)
            {
                programPieces.Add(Instructions.Operations.Subtract(token));
            }
            else if (token.Word.Value is Tokens.Operator.Multiply)
            {
                programPieces.Add(Instructions.Operations.Multiply(token));
            }
            else if (token.Word.Value is Tokens.Operator.Divide)
            {
                programPieces.Add(Instructions.Operations.Divide(token));
            }
            else if (token.Word.Value is Tokens.Operator.Modulo)
            {
                programPieces.Add(Instructions.Operations.Modulo(token));
            }
            else if (token.Word.Value is Tokens.Operator.Xor)
            {
                programPieces.Add(Instructions.Operations.Xor(token));
            }
            else if (token.Word.Value is Tokens.Operator.Or)
            {
                programPieces.Add(Instructions.Operations.Or(token));
            }
            else if (token.Word.Value is Tokens.Operator.And)
            {
                programPieces.Add(Instructions.Operations.And(token));
            }
            else if (token.Word.Value is Tokens.Operator.Equal)
            {
                programPieces.Add(Instructions.Operations.Equal(token));
            }
            else if (token.Word.Value is Tokens.Operator.ShiftLeft)
            {
                programPieces.Add(Instructions.Operations.ShiftLeft(token));
            }
            else if (token.Word.Value is Tokens.Operator.ShiftRight)
            {
                programPieces.Add(Instructions.Operations.ShiftRight(token));
            }
            else if (token.Word.Value is Tokens.Operator.StringEqual)
            {
                programPieces.Add(Instructions.Operations.StringEqual(token));
            }
            else if (token.Word.Value is Tokens.Operator.NotEqual)
            {
                programPieces.Add(Instructions.Operations.NotEqual(token));
            }
            else if (token.Word.Value is Tokens.Operator.LessThan)
            {
                programPieces.Add(Instructions.Operations.LessThan(token));
            }
            else if (token.Word.Value is Tokens.Operator.LessThanOrEqual)
            {
                programPieces.Add(Instructions.Operations.LessThanOrEqual(token));
            }
            else if (token.Word.Value is Tokens.Operator.GreaterThan)
            {
                programPieces.Add(Instructions.Operations.GreaterThan(token));
            }
            else if (token.Word.Value is Tokens.Operator.GreaterThanOrEqual)
            {
                programPieces.Add(Instructions.Operations.GreaterThanOrEqual(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Print)
            {
                programPieces.Add(Instructions.Print(token));
            }
            else if (token.Word.Value is Tokens.Keyword.PrintChar)
            {
                programPieces.Add(Instructions.PrintChar(token));
            }
            else if (token.Word.Value is Tokens.Keyword.PrintString)
            {
                programPieces.Add(Instructions.PrintString(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Over)
            {
                programPieces.Add(Instructions.Over(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Drop)
            {
                programPieces.Add(Instructions.Drop(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Swap)
            {
                programPieces.Add(Instructions.Swap(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Branch)
            {
                programPieces.Add(ParseBranches(token, tokens, meta, context));
            }
            else if (token.Word.Value is Tokens.Keyword.Loop)
            {
                programPieces.Add(ParseLoop(token, tokens, meta, context));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall0)
            {
                programPieces.Add(Instructions.Syscall(0, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall1)
            {
                programPieces.Add(Instructions.Syscall(1, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall2)
            {
                programPieces.Add(Instructions.Syscall(2, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall3)
            {
                programPieces.Add(Instructions.Syscall(3, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall4)
            {
                programPieces.Add(Instructions.Syscall(4, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall5)
            {
                programPieces.Add(Instructions.Syscall(5, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Syscall6)
            {
                programPieces.Add(Instructions.Syscall(6, token));
            }
            else if (token.Word.Value is Tokens.Keyword.Allocate)
            {
                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected `[` after {token}, but got nothing.");
                }
                var expectedBracket = tokens.Dequeue();
                if (expectedBracket.Word.Value is not "[")
                {
                    throw new Exception($"Expected `[` after {token}, but got {expectedBracket}");
                }

                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected size or struct after {expectedBracket}, but got nothing.");
                }

                var sizeOrStruct = tokens.Dequeue();
                if (int.TryParse(sizeOrStruct.Word.Value, out var size))
                {
                }
                else if (context.TryGetStructure(sizeOrStruct.Word.Value, out var structure))
                {
                    size = structure.Size;
                }
                else
                {
                    throw new Exception($"Expected size or struct after {expectedBracket}, but got {sizeOrStruct}");
                }

                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected `]` after {sizeOrStruct}, but got nothing.");
                }
                expectedBracket = tokens.Dequeue();
                if (expectedBracket.Word.Value is not "]")
                {
                    throw new Exception($"Expected `]` after {sizeOrStruct}, but got {expectedBracket}");
                }

                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected label for memory after {expectedBracket}, but got nothing.");
                }
                var label = tokens.Dequeue();
                var uniqueLabel = context.AddMemory(label);
                meta.AddMemory(uniqueLabel, size);
            }
            else if (token.Word.Value is Tokens.Keyword.Duplicate)
            {
                programPieces.Add(Instructions.Duplicate(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Exit)
            {
                programPieces.Add(Instructions.Exit(token));
            }
            else if (token.Word.Value is Tokens.Keyword.PinStackElements)
            {
                var pins = new List<PinnedStackItem>();
                var toBePinned = new Stack<Token>();
                while (tokens.Count > 0)
                {
                    var pinToken = tokens.Dequeue();
                    if (pinToken.Word.Value is ":")
                    {
                        break;
                    }
                    if (context.IsReserved(pinToken.Word.Value))
                    {
                        throw new Exception($"Cannot use reserved keyword {pinToken} as a pinned stack item.");
                    }
                    toBePinned.Push(pinToken);
                }
                while (toBePinned.Count > 0)
                {
                    var pinnedStackItem = meta.PinStackItem(toBePinned.Pop());
                    programPieces.Add(Instructions.PinStackItem(pinnedStackItem));
                    pins.Add(pinnedStackItem);
                }
                var program = ParseProgram(tokens, meta, context, ";");
                if (tokens.Count is 0)
                {
                    throw new Exception($"Unclosed using block.");
                }
                var finalToken = tokens.Dequeue();
                if (finalToken.Word.Value is not ";")
                {
                    throw new Exception($"Expected `;` after {token}, but got {finalToken}");
                }
                programPieces.AddRange(program);
                foreach (var pin in pins)
                {
                    meta.UnpinStackItem(pin.Token.Word.Value);
                }
            }
            else if (context.TryGetMemory(token, out var memoryLabel))
            {
                programPieces.Add(Instructions.PushMemoryPointer(token, memoryLabel));
            }
            else if (token.Word.Value.Contains('.'))
            {
                var parts = token.Word.Value.Split('.');
                if (parts.Length > 2)
                {
                    throw new Exception($"Unknown token {token}");
                }
                var structName = parts[0];
                var fieldName = parts[1];
                if (!context.TryGetStructure(structName, out var structure))
                {
                    throw new Exception($"Unknown structure {structName}.");
                }
                if (!structure.Fields.TryGetValue(fieldName, out var field))
                {
                    throw new Exception($"Unknown member {fieldName}.");
                }
                programPieces.Add(Instructions.StructFieldOffset(token, field.Offset));
            }
            else if (token.Word.Value is Tokens.Keyword.Load)
            {
                programPieces.Add(Instructions.Load(token));
            }
            else if (token.Word.Value is Tokens.Keyword.LoadByte)
            {
                programPieces.Add(Instructions.LoadByte(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Store)
            {
                programPieces.Add(Instructions.Store(token));
            }
            else if (token.Word.Value is Tokens.Keyword.Inspect)
            {
                var stack = new TypeStack();
                foreach (var programPiece in programPieces)
                {
                    programPiece.TypeCheck(stack);
                }
                Console.WriteLine(stack);
            }
            else if (token.Word.Value is Tokens.Keyword.Cast)
            {
                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected type after {token}, but got nothing.");
                }
                var typeToken = tokens.Dequeue();
                if (typeToken.Word.Value is not Tokens.DataType.Number and not Tokens.DataType.Pointer)
                {
                    throw new Exception($"Expected type after {token}, but got {typeToken}.");
                }
                if (typeToken.Word.Value is Tokens.DataType.Number)
                {
                    programPieces.Add(Instructions.Cast(token, DataType.Number));
                }
                else if (typeToken.Word.Value is Tokens.DataType.Pointer)
                {
                    programPieces.Add(Instructions.Cast(token, DataType.Pointer));
                }
            }
            else
            {
                throw new Exception($"Unknown token {token}");
            }
        }

        return programPieces;
    }

    private static Loop ParseLoop(Token token, Queue<Token> tokens, GlobalContext meta, Context context)
    {
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected loop condition, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var iteratorLabel = tokens.Dequeue();
        if (context.IsReserved(iteratorLabel.Word.Value))
        {
            throw new Exception($"Cannot use reserved keyword {iteratorLabel} as a loop iterator.");
        }

        var iterator = meta.PinStackItem(iteratorLabel);

        if (tokens.Count is 0)
        {
            throw new Exception($"Expected condition after loop iterator, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var condition = ParseProgram(tokens, meta, context, ":");
        var endToken = tokens.Dequeue();
        if (endToken.Word.Value is not ":")
        {
            throw new Exception($"Expected `:` after loop condition, but got {endToken}");
        }
        var body = ParseProgram(tokens, meta, context, ";");
        var endBodyToken = tokens.Dequeue();
        if (endBodyToken.Word.Value is not ";")
        {
            throw new Exception($"Expected `;` after loop body, but got {endBodyToken}");
        }

        meta.UnpinStackItem(iteratorLabel.Word.Value);
        return new(token, iterator, condition, body);
    }


    private static Branching ParseBranches(Token token, Queue<Token> tokens, GlobalContext meta, Context context, string? modifier = null)
    {
        List<IProgramPiece>? yesBranch = null;
        List<IProgramPiece>? noBranch = null;

        if (tokens.Count is 0)
        {
            throw new Exception($"Expected yes: or no: after {token}, but got nothing.");
        }
        var firstBranch1Token = tokens.Dequeue();
        if (firstBranch1Token.Word.Value is not "yes" and not "no")
        {
            throw new Exception($"Expected `yes:` or `no:` after ?, but got {firstBranch1Token}");
        }
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected `:` after {firstBranch1Token}, but got nothing.");
        }
        var firstBranchBlockStartToken = tokens.Dequeue();
        if (firstBranchBlockStartToken.Word.Value is not ":")
        {
            throw new Exception($"Expected `:` after {firstBranch1Token}, but got {firstBranchBlockStartToken}");
        }
        var branch1Program = ParseProgram(tokens, meta, context, ";");
        var branchEndToken = tokens.Dequeue();
        if (branchEndToken.Word.Value is not ";")
        {
            throw new Exception($"Expected `;` after {firstBranch1Token}, but got {branchEndToken}");
        }

        if (firstBranch1Token.Word.Value is "yes")
        {
            yesBranch = branch1Program;
        }
        else
        {
            noBranch = branch1Program;
        }

        var expectedBranch2Token = firstBranch1Token.Word.Value is "yes" ? "no" : "yes";

        if (tokens.Count < 3 || tokens.Peek().Word.Value != expectedBranch2Token)
        {
            return new(token, yesBranch, noBranch);
        }

        var firstBranch2Token = tokens.Dequeue();

        if (tokens.Peek().Word.Value is not ":")
        {
            // TODO: clean this shit up
            var newTokens = new Queue<Token>();
            newTokens.Enqueue(firstBranch2Token);
            while (tokens.Count > 0)
            {
                newTokens.Enqueue(tokens.Dequeue());
            }
            while (newTokens.Count > 0)
            {
                tokens.Enqueue(newTokens.Dequeue());
            }

            return new(token, yesBranch, noBranch);
        }
        tokens.Dequeue();
        var branch2Program = ParseProgram(tokens, meta, context, ";");
        var endBranch2Token = tokens.Dequeue();
        if (endBranch2Token.Word.Value is not ";")
        {
            throw new Exception($"Expected `;` after {firstBranch2Token}, but got {endBranch2Token}");
        }


        if (firstBranch1Token.Word.Value is "yes")
        {
            noBranch = branch2Program;
        }
        else
        {
            yesBranch = branch2Program;
        }
        return new(token, yesBranch, noBranch);
    }

    private static List<DataType> ParseDataTypes(Queue<Token> tokens, string? terminationToken = null)
    {
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected contract, but got nothing.");
        }

        var types = new List<DataType>();

        while (tokens.Count > 0 && tokens.Peek().Word.Value != terminationToken)
        {
            var token = tokens.Dequeue();

            if (token.Word.Value is Tokens.DataType.Number)
            {
                types.Add(DataType.Number);
            }
            else if (token.Word.Value is Tokens.DataType.Pointer)
            {
                types.Add(DataType.Pointer);
            }
            else if (token.Word.Value is Tokens.DataType.Boolean)
            {
                types.Add(DataType.Number);
            }
            else if (token.Word.Value is Tokens.DataType.String)
            {
                types.Add(DataType.Number);
                types.Add(DataType.Pointer);
            }
            else if (token.Word.Value is Tokens.DataType.NullTerminatedString)
            {
                types.Add(DataType.Pointer);
            }
            else
            {
                throw new Exception($"Unknown type {token}");
            }
        }

        return types;
    }

    private static Context ParseInclude(Token token, Queue<Token> tokens, GlobalContext meta)
    {
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected path after include, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var includePath = tokens.Dequeue();
        if (includePath.Word is not StringLiteralWord stringLiteralWord)
        {
            throw new Exception($"Expected path after include, but got {includePath}");
        }
        var path = stringLiteralWord.InnerValue;
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new Exception($"Cannot include {path}({fullPath}) because it does not exist at {token}.");
        }
        Console.WriteLine($"Including {path}");
        var words = LexFile(path);

        return MetaEvaluate(words, meta, new(), null, out _, out _);
    }

    private static Constant ParseConstant(Token token, Queue<Token> tokens, Context context)
    {
        if (tokens.Count < 2)
        {
            throw new Exception($"Expected at least two tokens after `aka`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var nameToken = tokens.Dequeue();
        if (context.IsReserved(nameToken.Word.Value))
        {
            throw new Exception($"Expected identifier, but got an existing keyword {nameToken.Word}.");
        }
        var constant = tokens.Dequeue();
        if (int.TryParse(constant.Word.Value, out var constInt))
        {
            return new(nameToken, DataType.Number, Number: constInt);
        }
        else if (constant.Word is StringLiteralWord constString)
        {
            return new(nameToken, DataType.String, Text: constString.Value);
        }
        else if (constant.Word is NullTerminatedStringLiteralWord constZeroString)
        {
            return new(nameToken, DataType.ZeroTerminatedString, Text: constZeroString.Value);
        }
        else
        {
            throw new Exception($"Expected number after `aka`, but got {constant} @ {constant.Filename}:{constant.Line}:{constant.Column}");
        }
    }

    private static Structure ParseStructure(Token token, Queue<Token> tokens, Context context)
    {
        var structName = tokens.Dequeue();
        var fields = new Dictionary<string, StructureField>();

        if (context.IsReserved(structName.Word.Value))
        {
            throw new Exception($"Expected identifier, but got an existing keyword {structName}.");
        }

        if (tokens.Count is 0)
        {
            throw new Exception($"Expected block, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }
        tokens.Dequeue();

        var offset = 0;
        while (tokens.Count > 0)
        {
            var member = tokens.Dequeue();
            if (member.Word.Value is ";")
            {
                break;
            }

            var sizeToken = tokens.Dequeue();
            if (sizeToken.Word.Value is ";")
            {
                break;
            }

            if (!int.TryParse(sizeToken.Word.Value, out var size))
            {
                throw new Exception($"Expected integer, but got {sizeToken}.");
            }

            if (tokens.Count is 0)
            {
                throw new Exception($"Expected identifier, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }

            fields.Add(member.Word.Value, new(offset, size, member.Word.Value));
            offset += size;

            if (tokens.Count is 0)
            {
                throw new Exception($"Unclosed struct definition @ {token.Filename}:{token.Line}:{token.Column}");
            }
        }
        return new Structure(structName, fields);
    }
}

internal record Structure(Token Token, Dictionary<string, StructureField> Fields)
{
    public string Name => Token.Word.Value;
    internal int Size => Fields.Sum(f => f.Value.Size);
}

internal record StructureField(int Offset, int Size, string Name);

enum DataType
{
    Number,
    Pointer,
    String,
    ZeroTerminatedString,
    Unknown,
}
