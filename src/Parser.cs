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
                innerContext.AddConstant(ParseConstant(token, tokenQueue, innerContext, meta));
            }
            else if (tokenQueue.Count > 0 && tokenQueue.Peek().Word.Value is "(")
            {
                var functionName = token.Word.Value;
                tokenQueue.Dequeue();
                if (innerContext.IsReserved(functionName))
                {
                    throw new Exception($"Cannot use reserved keyword {token} as a function name.");
                }

                var arguments = ParseParameters(innerContext, tokenQueue, ")");
                List<ParameterType> inputPins = [];
                if (arguments.All(Parameter.IsNamed))
                {
                    inputPins = arguments;
                }
                else if (arguments.Any(Parameter.IsNamed))
                {
                    throw new Exception($"Expected either all anonymous parameters or all named parameters for function input, but got mix in {token}");
                }
                var argumentsEndToken = tokenQueue.Dequeue();
                if (argumentsEndToken.Word.Value is not ")")
                {
                    throw new Exception($"Expected `)` after function arguments, but got {argumentsEndToken}.");
                }

                var output = ParseParameters(innerContext, tokenQueue, ":");
                if (output.Any(Parameter.IsNamed))
                {
                    throw new Exception($"Expected anonymous parameters for function output, but got named parameters.");
                }
                var outputEndToken = tokenQueue.Dequeue();
                if (outputEndToken.Word.Value is not ":")
                {
                    throw new Exception($"Expected `:` after function output, but got {outputEndToken}.");
                }

                var contract = new Contract([.. arguments.Select(a => a.Typing)], [.. output.Select(o => o.Typing)]);
                var functionContext = MetaEvaluate(tokenQueue, meta, innerContext, ";", out var innerRemaining, out var remaining);
                functionContext.Name = functionName;
                tokenQueue = (Queue<Token>)remaining;
                var endToken = tokenQueue.Dequeue();
                if (endToken.Word.Value is not ";")
                {
                    throw new Exception($"Expected `;` after function, but got {endToken}.");
                }
                var functionMeta = new FunctionMeta(token, innerRemaining.ToList(), contract, inputPins, functionContext);
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
                List<IProgramPiece> funcProgram = [];
                var pinnedInputItems = func.InputPins.Reverse<ParameterType>()
                    .Select(p => meta.PinStackItem(p.GetNameToken(), p.Typing))
                    .ToList();

                pinnedInputItems.ForEach(item => funcProgram.Add(Instructions.PinStackItem(item)));

                funcProgram.AddRange(ParseProgram(new Queue<Token>(func.Body), meta, func.Context, ";"));


                var parsedFunc = new Function(token, func.Contract, func.InputPins.Count is not 0, funcProgram);
                programPieces.Add(parsedFunc);

                pinnedInputItems.ForEach(item => meta.UnpinStackItem(item.Token));
            }
            else if (meta.PinnedStackItems.TryGetValue(token.Word.Value, out var pinnedStackItems))
            {
                programPieces.Add(Instructions.PushPinnedStackItem(pinnedStackItems.Peek()));
            }
            else if (context.TryGetConstant(token.Word.Value, out var constant))
            {
                if (constant.Type is ConstantTypes.String)
                {
                    var str = meta.StringLiterals[constant.Text!];
                    programPieces.Add(Instructions.Literal.String(token, str));
                }
                else if (constant.Type is ConstantTypes.ZeroTerminatedString)
                {
                    var zstr = meta.NullTerminatedStringLiterals[constant.Text!];
                    programPieces.Add(Instructions.Literal.NullTerminatedString(token, zstr.Index));
                }
                else if (constant.Type is ConstantTypes.Number)
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
            else if (Tokens.Primitive.TryParsePrimitive(token.Word.Value, out var dataType))
            {
                ParseTypedAllocation(tokens, meta, context, token, Typing.Create(dataType));
            }
            else if (context.TryGetStructure(token.Word.Value, out var structure))
            {
                ParseTypedAllocation(tokens, meta, context, token, Typing.Create(structure));
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

                var sizeToken = tokens.Dequeue();
                if (!int.TryParse(sizeToken.Word.Value, out var size))
                {
                    throw new Exception($"Expected size or struct after {expectedBracket}, but got {sizeToken}");
                }

                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected `]` after {sizeToken}, but got nothing.");
                }
                expectedBracket = tokens.Dequeue();
                if (expectedBracket.Word.Value is not "]")
                {
                    throw new Exception($"Expected `]` after {sizeToken}, but got {expectedBracket}");
                }

                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected label for memory after {expectedBracket}, but got nothing.");
                }
                var label = tokens.Dequeue();
                var memoryAllocation = context.AddMemory(label, Typing.Create(Primitives.Number), size);
                meta.AddMemory(memoryAllocation);
            }
            else if (token.Word.Value is Tokens.Keyword.Repeat)
            {
                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected iterator label or `:` after {token}, but got nothing.");
                }
                var next = tokens.Dequeue();
                PinnedStackItemType iteration;
                if (next.Word.Value is not ":")
                {
                    iteration = meta.PinStackItem(next, Typing.Create(Primitives.Number));

                    if (tokens.Count is 0)
                    {
                        throw new Exception($"Expected `:` after {next}, but got nothing.");
                    }
                    tokens.Dequeue();
                }
                else
                {
                    iteration = meta.PinStackItem(Token.OnlyValue("i"), Typing.Create(Primitives.Number));
                }
                // Iterator starts at 0
                programPieces.Add(Instructions.Literal.Number(token, 0));

                var repeatProgram = ParseProgram(tokens, meta, context, ";");
                repeatProgram.Add(Instructions.PushPinnedStackItem(iteration));
                repeatProgram.Add(Instructions.Literal.Number(token, 1));
                repeatProgram.Add(Instructions.Operations.Add(token));
                if (tokens.Count is 0)
                {
                    throw new Exception($"Unclosed repeat block.");
                }
                var finalToken = tokens.Dequeue();
                if (finalToken.Word.Value is not ";")
                {
                    throw new Exception($"Expected `;` after {token}, but got {finalToken}");
                }

                List<IProgramPiece> conditionalProgram = [
                    Instructions.Over(token),
                    Instructions.Operations.LessThan(token)
                ];

                programPieces.Add(new Loop(token, iteration, conditionalProgram, repeatProgram));

                // Drop the last iterator value and the given count to repeat
                programPieces.Add(Instructions.Drop(token));
                programPieces.Add(Instructions.Drop(token));

                meta.UnpinStackItem(iteration.Token);
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
                var pins = new List<PinnedStackItemType>();
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
                    var pinnedStackItem = meta.PinStackItem(toBePinned.Pop(), Typing.Create(Primitives.Runtime));
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
                    meta.UnpinStackItem(pin.Token);
                }
            }
            else if (context.TryGetMemory(token.Word.Value, out var memoryAllocation))
            {
                OffsetType offset = Offset.Create(0);
                if (tokens.Count >= 3 && tokens.Peek().Word.Value is "[")
                {
                    if (memoryAllocation.Count is 1)
                    {
                        throw new Exception($"Cannot index into memory allocation {memoryAllocation.GetAssemblyLabel()} because it is not an array.");
                    }
                    var bracketToken = tokens.Dequeue();
                    var indexToken = tokens.Dequeue();
                    tokens.Dequeue();
                    if (!int.TryParse(indexToken.Word.Value, out var index))
                    {
                        throw new Exception($"Expected number after {bracketToken}, but got {indexToken}");
                    }

                    if (index >= memoryAllocation.Count)
                    {
                        throw new Exception($"Index {index} is out of bounds for memory allocation {memoryAllocation.GetAssemblyLabel()}.");
                    }

                    if (tokens.Count >= 2 && tokens.Peek().Word.Value.StartsWith('.'))
                    {
                        var fieldNameToken = tokens.Dequeue();
                        var fieldName = fieldNameToken.Word.Value[1..];
                        programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, index, fieldName));
                    }
                    else
                    {
                        programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, index));
                    }
                }
                else
                {
                    programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, 0));
                }
            }
            else if (token.Word.Value.Contains('.'))
            {
                var parts = token.Word.Value.Split('.');
                if (parts.Length > 2)
                {
                    throw new Exception($"Unknown token {token}");
                }
                var name = parts[0];
                var fieldName = parts[1];
                if (context.TryGetStructure(name, out var structureDefinition))
                {
                    if (!structureDefinition.Fields.TryGetValue(fieldName, out var field))
                    {
                        throw new Exception($"Unknown member {fieldName}.");
                    }
                    programPieces.Add(Instructions.StructFieldOffset(token, field.Offset));
                    continue;
                }
                else if (context.TryGetMemory(name, out var memory))
                {
                    programPieces.Add(Instructions.PushMemoryPointer(token, memory, 0, fieldName));
                    continue;
                }
                else if (meta.PinnedStackItems.TryGetValue(name, out var pinnedItem))
                {
                    programPieces.Add(Instructions.PushFieldOfPinnedStackItem(pinnedItem.Peek(), fieldName));
                    continue;
                }
                else
                {
                    throw new Exception($"Unknown structure {name}.");
                }

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
                    programPiece.TypeCheck(stack, []);
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
                if (typeToken.Word.Value is not Tokens.Primitive.Number and not Tokens.Primitive.Pointer)
                {
                    throw new Exception($"Expected type after {token}, but got {typeToken}.");
                }
                if (typeToken.Word.Value is Tokens.Primitive.Number)
                {
                    programPieces.Add(Instructions.Cast(token, Primitives.Number));
                }
                else if (typeToken.Word.Value is Tokens.Primitive.Pointer)
                {
                    programPieces.Add(Instructions.Cast(token, Primitives.Pointer));
                }
            }
            else
            {
                throw new Exception($"Unknown token {token}");
            }
        }

        return programPieces;
    }

    private static void ParseTypedAllocation(Queue<Token> tokens, GlobalContext meta, Context context, Token token, TypingType typing)
    {
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected identifier after {token}, but got nothing.");
        }

        var next = tokens.Dequeue();
        var identifier = next;
        var count = 1;
        if (next.Word.Value is "[")
        {
            var countToken = tokens.Dequeue();
            if (tokens.Count is 0)
            {
                throw new Exception($"Expected count after {countToken}, but got nothing.");
            }
            count = int.Parse(countToken.Word.Value);
            if (tokens.Count is 0)
            {
                throw new Exception($"Expected `]` after {countToken}, but got nothing.");
            }
            tokens.Dequeue();

            identifier = tokens.Dequeue();
        }
        var memoryLabel = context.AddMemory(identifier, typing, count);

        meta.AddMemory(memoryLabel);
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

        var iterator = meta.PinStackItem(iteratorLabel, Typing.Create(Primitives.Runtime));

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

        meta.UnpinStackItem(iteratorLabel);
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

    private static List<ParameterType> ParseParameters(Context context, Queue<Token> tokens, string? terminationToken = null)
    {
        if (tokens.Count is 0)
        {
            throw new Exception($"Expected contract, but got nothing.");
        }

        var parameters = new List<ParameterType>();

        while (tokens.Count > 0 && tokens.Peek().Word.Value != terminationToken)
        {
            var token = tokens.Dequeue();
            if (!TryParseTyping(context, token, out var typing))
            {
                throw new Exception($"Expected type, but got {token}.");
            }

            if (tokens.Count is 0
            || TryParseTyping(context, tokens.Peek(), out _)
            || tokens.Peek().Word.Value == terminationToken)
            {
                parameters.Add(Parameter.Create(typing));
                continue;
            }

            var nameToken = tokens.Dequeue();

            if (context.IsReserved(nameToken.Word.Value))
            {
                throw new Exception($"Cannot use reserved keyword {nameToken} as a name for input.");
            }

            parameters.Add(Parameter.Create(nameToken, typing));
        }

        return parameters;
    }

    private static bool TryParseTyping(Context context, Token token, out TypingType typing)
    {
        typing = token.Word.Value switch
        {
            _ when Tokens.Primitive.TryParsePrimitive(token.Word.Value, out var dataType) => Typing.Create(dataType),
            _ when context.TryGetStructure(token.Word.Value, out var structure) => Typing.Create(structure),
            _ => null!,
        };
        return typing is not null;
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

    private static Constant ParseConstant(Token token, Queue<Token> tokens, Context context, GlobalContext meta)
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
            return new(nameToken, ConstantTypes.Number, Number: constInt);
        }
        else if (constant.Word is StringLiteralWord constString)
        {
            meta.StringLiterals[constString.Value] = new(constString.InnerValue, stringLiteralCounter++);
            return new(nameToken, ConstantTypes.String, Text: constString.Value);
        }
        else if (constant.Word is NullTerminatedStringLiteralWord constZeroString)
        {
            meta.NullTerminatedStringLiterals[constZeroString.Value] = new(constZeroString.InnerValue, nullTerminatedStringLiteralCounter++);
            return new(nameToken, ConstantTypes.ZeroTerminatedString, Text: constZeroString.Value);
        }
        else
        {
            throw new Exception($"Expected number/string after `aka`, but got {constant} @ {constant.Filename}:{constant.Line}:{constant.Column}");
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

            var type = Primitives.Unknown;
            if (int.TryParse(sizeToken.Word.Value, out var size))
            {
            }
            else if (TryParseTyping(context, sizeToken, out var typing))
            {
                size = typing.GetSize();
                type = typing.ToPrimitives().First();
            }
            else
            {
                throw new Exception($"Expected integer, but got {sizeToken}.");
            }

            if (tokens.Count is 0)
            {
                throw new Exception($"Expected identifier, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }

            fields.Add(member.Word.Value, new(offset, size, type, member.Word.Value));
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

    public static Structure String { get; } = new(Token.OnlyValue("str"), new()
    {
        ["length"] = new StructureField(0, 8, Primitives.Number, "length"),
        ["start"] = new StructureField(8, 8, Primitives.Pointer, "start"),
    });

    public static Structure ZeroTerminatedString { get; } = new(Token.OnlyValue("0str"), new()
    {
        ["start"] = new StructureField(0, 8, Primitives.Pointer, "start"),
    });

    public Primitives[] Decompose() => Fields.Values
        .OrderBy(f => f.Offset)
        .Select(f => f.Type)
        .ToArray();
}

internal record StructureField(int Offset, int Size, Primitives Type, string Name);

public enum Primitives
{
    Number,
    Pointer,
    Unknown,
    Runtime,
}


internal abstract record ParameterType(TypingType Typing);
internal record NamedParameter(Token Name, TypingType Typing) : ParameterType(Typing);
internal record AnonymousParameter(TypingType Typing) : ParameterType(Typing);

internal static class Parameter
{
    internal static ParameterType Create(Token nameToken, Primitives primitive) => new NamedParameter(nameToken, Typing.Create(primitive));
    internal static ParameterType Create(Primitives privmitive) => new AnonymousParameter(Typing.Create(privmitive));
    internal static ParameterType Create(TypingType typing) => new AnonymousParameter(typing);
    internal static ParameterType Create(Token nameToken, TypingType typing) => new NamedParameter(nameToken, typing);

    internal static bool IsNamed(this ParameterType parameter) => parameter is NamedParameter;

    internal static Token GetNameToken(this ParameterType parameter) => parameter switch
    {
        NamedParameter namedParameter => namedParameter.Name,
        _ => throw new Exception("No name for this parameter."),
    };
}

internal abstract record TypingType;

internal record PrimitiveType(Primitives DataType) : TypingType;
internal record ComplexType(Structure Structure) : TypingType;
internal record PointerType(TypingType InnerType) : TypingType;

internal static class Typing
{
    internal static TypingType Create(Primitives dataType) => new PrimitiveType(dataType);
    internal static TypingType Create(Structure structure) => structure.Fields.Count is 1 ? new PrimitiveType(structure.Fields.First().Value.Type) : new ComplexType(structure);
    internal static TypingType CreatePointer(TypingType innerType) => new PointerType(innerType);

    internal static bool IsPointer(this TypingType type) => type switch
    {
        PointerType _ => true,
        PrimitiveType primitive => primitive.DataType is Primitives.Pointer,
        _ => false,
    };

    internal static TypingType GetInnerType(this TypingType type) => type switch
    {
        PointerType pointer => pointer.InnerType,
        PrimitiveType primitive => primitive.DataType switch
        {
            Primitives.Pointer => Create(Primitives.Number),
            _ => throw new Exception("Not a pointer."),
        },
        _ => throw new Exception("Not a pointer."),
    };

    internal static int GetSize(this TypingType type) => type switch
    {
        PrimitiveType primitive => primitive.DataType switch
        {
            Primitives.Number => 8,
            Primitives.Pointer => 8,
            Primitives.Runtime => 8,
            _ => throw new Exception($"Unknown data type {primitive.DataType}."),
        },
        ComplexType complex => complex.Structure.Size,
        _ => throw new Exception("Unknown type."),
    };

    internal static OffsetType GetOffsetOf(this TypingType type, string fieldName) => type switch
    {
        ComplexType complex => Offset.Create(complex.Structure.Fields[fieldName].Offset),
        _ => throw new Exception("Primitives have no fields."),
    };

    internal static StructureField GetField(this TypingType type, string fieldName) => type switch
    {
        ComplexType complex => complex.Structure.Fields[fieldName],
        _ => throw new Exception("Primitives have no fields."),
    };

    internal static TypingType Add(this TypingType type, TypingType other)
    {
        if (type is ComplexType || other is ComplexType)
        {
            throw new Exception("Expected both types to be primitive.");
        }

        if (type.IsPointer() && other.IsPointer())
        {
            throw new Exception("Cannot add two pointers.");
        }

        if (type.IsPointer() || other.IsPointer())
        {
            // We don't know what type of pointer this is anymore.
            return Create(Primitives.Pointer);
        }
        else
        {
            return Create(Primitives.Number);
        }
    }

    internal static TypingType Subtract(this TypingType type, TypingType other)
    {
        if (type is ComplexType || other is ComplexType)
        {
            throw new Exception("Expected both types to be primitive.");
        }

        if (type.IsPointer() && other.IsPointer())
        {
            return Create(Primitives.Number);
        }
        else if (type.IsPointer() || other.IsPointer())
        {
            return Create(Primitives.Pointer);
        }
        else
        {
            return Create(Primitives.Number);
        }
    }

    internal static TypingType[] Decompose(this IEnumerable<TypingType> types)
    {
        var decomposed = new List<TypingType>();
        foreach (var type in types)
        {
            if (type is ComplexType complex)
            {
                decomposed.AddRange(complex.Structure.Fields.Values.Select(f => Create(f.Type)));
            }
            else
            {
                decomposed.Add(type);
            }
        }
        return [.. decomposed];
    }

    internal static Primitives[] ToPrimitives(this TypingType type) => type switch
    {
        PrimitiveType primitive => [primitive.DataType],
        PointerType pointer => [Primitives.Pointer],
        ComplexType complex => complex.Structure.Fields.Values
            .OrderBy(f => f.Offset)
            .Select(f => f.Type)
            .ToArray(),
        _ => throw new Exception("Unknown type."),
    };
}

internal abstract record OffsetType;
internal record IntOffsetType(int Offset) : OffsetType;

internal static class Offset
{
    internal static OffsetType Create(int offset) => new IntOffsetType(offset);
    internal static int GetValue(this OffsetType offset) => offset switch
    {
        IntOffsetType intOffset => intOffset.Offset,
        _ => throw new Exception("Unknown offset type."),
    };
}
