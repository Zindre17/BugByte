namespace BugByte;

internal static class Parser
{
    public static List<IProgramPiece> ParseProgram(SourceCode code, Definitions definitions, string? terminationToken = null)
    {
        if (!code.HasNextToken())
        {
            if (code.HasEnumerationStarted())
            {
                throw new Exception($"Expected tokens, but got nothing after {code.CurrentToken()}.");
            }
            else
            {
                throw new Exception("Expected tokens, but got nothing.");
            }
        }

        var programPieces = new List<IProgramPiece>();

        while (code.HasNextToken() && code.PeekNextToken().Word.Value != terminationToken)
        {
            var token = code.MoveNext();
            if (int.TryParse(token.Word.Value, out var number))
            {
                programPieces.Add(Instructions.Literal.Number(token, number));
            }
            else if (token.Word is StringLiteralWord stringLiteralWord)
            {
                programPieces.Add(Instructions.Literal.String(token, stringLiteralWord.InnerValue));
            }
            else if (token.Word is ZeroTerminatedStringLiteralWord zeroTerminatedStringLiteralWord)
            {
                programPieces.Add(Instructions.Literal.ZeroTerminatedString(token, zeroTerminatedStringLiteralWord.InnerValue));
            }
            else if (token.Word.Value is Tokens.Keyword.Yes)
            {
                programPieces.Add(Instructions.Boolean.Yes(token));
            }
            else if (token.Word.Value is Tokens.Keyword.No)
            {
                programPieces.Add(Instructions.Boolean.No(token));
            }
            else if (definitions.TryGetFunction(token.Word.Value, out var func))
            {
                programPieces.AddRange(func.Parse(definitions).Body);
            }
            else if (definitions.TryGetPin(token, out var pinnedStackItems))
            {
                programPieces.Add(Instructions.PushPinnedStackItem(pinnedStackItems.Current));
            }
            else if (definitions.TryGetConstant(token, out var constantDefinition))
            {
                var constant = constantDefinition.Parse(definitions);
                if (constant.Type is ConstantTypes.String)
                {
                    programPieces.Add(Instructions.Literal.String(token, constant.Text!));
                }
                else if (constant.Type is ConstantTypes.ZeroTerminatedString)
                {
                    programPieces.Add(Instructions.Literal.ZeroTerminatedString(token, constant.Text!));
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
                programPieces.Add(ParseBranches(code, definitions));
            }
            else if (token.Word.Value is Tokens.Keyword.Loop)
            {
                programPieces.Add(ParseLoop(code, definitions));
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
                ParseTypedAllocation(code, definitions, token, Typing.Create(dataType));
            }
            else if (token.Word.Value == Structure.ZeroTerminatedString.Name)
            {
                ParseTypedAllocation(code, definitions, token, Typing.Create(Structure.ZeroTerminatedString));
            }
            else if (token.Word.Value == Structure.String.Name)
            {
                ParseTypedAllocation(code, definitions, token, Typing.Create(Structure.String));
            }
            else if (definitions.TryGetStructure(token, out var structureDefinition))
            {
                var structure = structureDefinition.Parse(definitions);
                ParseTypedAllocation(code, definitions, token, Typing.Create(structure));
            }
            else if (token.Word.Value is Tokens.Keyword.Allocate)
            {
                if (!code.HasNextToken())
                {
                    throw new Exception($"Expected `[` after {token}, but got nothing.");
                }
                var expectedBracket = code.MoveNext();
                if (expectedBracket.Word.Value is not "[")
                {
                    throw new Exception($"Expected `[` after {token}, but got {expectedBracket}");
                }

                if (!code.HasNextToken())
                {
                    throw new Exception($"Expected size or struct after {expectedBracket}, but got nothing.");
                }

                var sizeToken = code.MoveNext();
                if (!int.TryParse(sizeToken.Word.Value, out var size))
                {
                    throw new Exception($"Expected size or struct after {expectedBracket}, but got {sizeToken}");
                }

                if (!code.HasNextToken())
                {
                    throw new Exception($"Expected `]` after {sizeToken}, but got nothing.");
                }
                expectedBracket = code.MoveNext();
                if (expectedBracket.Word.Value is not "]")
                {
                    throw new Exception($"Expected `]` after {sizeToken}, but got {expectedBracket}");
                }

                if (!code.HasNextToken())
                {
                    throw new Exception($"Expected label for memory after {expectedBracket}, but got nothing.");
                }
                var label = code.MoveNext();
                definitions.AddMemory(label, Typing.Create(Primitives.Number), size);
            }
            else if (token.Word.Value is Tokens.Keyword.Repeat)
            {
                if (!code.HasNextToken())
                {
                    throw new Exception($"Expected iterator label or `:` after {token}, but got nothing.");
                }
                var next = code.MoveNext();
                NamedPin iteration;
                if (next.Word.Value is not ":")
                {
                    iteration = definitions.PinStackItem(next, Typing.Create(Primitives.Number));

                    if (!code.HasNextToken())
                    {
                        throw new Exception($"Expected `:` after {next}, but got nothing.");
                    }
                    code.MoveNext();
                }
                else
                {
                    iteration = definitions.PinStackItem(Token.OnlyValue("i"), Typing.Create(Primitives.Number));
                }
                // Iterator starts at 0
                programPieces.Add(Instructions.Literal.Number(token, 0));

                var repeatProgram = ParseProgram(code, definitions, Tokens.BlockEnd);
                repeatProgram.Add(Instructions.PushPinnedStackItem(iteration.Current));
                repeatProgram.Add(Instructions.Literal.Number(token, 1));
                repeatProgram.Add(Instructions.Operations.Add(token));
                if (!code.HasNextToken())
                {
                    throw new Exception($"Unclosed repeat block.");
                }
                var finalToken = code.MoveNext();
                if (finalToken.Word.Value is not ";")
                {
                    throw new Exception($"Expected `;` after {token}, but got {finalToken}");
                }

                List<IProgramPiece> conditionalProgram = [
                    Instructions.Over(token),
                    Instructions.Operations.LessThan(token)
                ];

                programPieces.Add(new Loop(token, iteration.Current, conditionalProgram, repeatProgram));

                // Drop the last iterator value and the given count to repeat
                programPieces.Add(Instructions.Drop(token));
                programPieces.Add(Instructions.Drop(token));

                iteration.Unpin();
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
                var pins = new Stack<NamedPin>();
                var toBePinned = new Stack<Token>();
                while (code.HasNextToken())
                {
                    var pinToken = code.MoveNext();
                    if (pinToken.Word.Value is ":")
                    {
                        break;
                    }
                    toBePinned.Push(pinToken);
                }
                while (toBePinned.Count > 0)
                {
                    var pinnedStackItem = definitions.PinStackItem(toBePinned.Pop(), Typing.Create(Primitives.Runtime));
                    programPieces.Add(Instructions.PinStackItem(pinnedStackItem.Current));
                    pins.Push(pinnedStackItem);
                }
                var program = ParseProgram(code, definitions, ";");
                if (code.HasNextToken() && code.MoveNext().Word.Value is not ";")
                {
                    throw new Exception($"Expected `;` after {token}, but got {code.CurrentToken()}");
                }
                programPieces.AddRange(program);
                foreach (var pin in pins)
                {
                    pin.Unpin();
                }
            }
            else if (definitions.TryGetMemory(token.Word.Value, out var memoryAllocation))
            {
                if (code.HasRemainingTokens(3) && code.PeekNextToken().Word.Value is "[")
                {
                    if (memoryAllocation.Count is 1)
                    {
                        throw new Exception($"Cannot index into memory allocation {memoryAllocation.GetAssemblyLabel()} because it is not an array.");
                    }
                    var bracketToken = code.MoveNext();
                    var indexToken = code.MoveNext();
                    var isDynamicIndex = false;
                    var index = 0;
                    if (indexToken.Word.Value is "]")
                    {
                        isDynamicIndex = true;
                    }
                    else
                    {
                        if (!int.TryParse(indexToken.Word.Value, out index))
                        {
                            throw new Exception($"Expected number after {bracketToken}, but got {indexToken}");
                        }
                        code.MoveNext(); // Consume the closing bracket
                    }

                    if (index >= memoryAllocation.Count)
                    {
                        throw new Exception($"Index {index} is out of bounds for memory allocation {memoryAllocation.GetAssemblyLabel()}.");
                    }

                    if (code.HasRemainingTokens(2) && code.PeekNextToken().Word.Value.StartsWith('.'))
                    {
                        var fieldNameToken = code.MoveNext();
                        var fieldName = fieldNameToken.Word.Value[1..];
                        programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, index, fieldName, isDynamicIndex));
                    }
                    else if (code.HasNextToken() && code.PeekNextToken().Word.Value is Tokens.Keyword.Load)
                    {
                        code.MoveNext();
                        programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, index, isDynamicIndex));
                        programPieces.Add(Instructions.LoadTyped(token, memoryAllocation.Typing));
                    }
                    else if (code.HasNextToken() && code.PeekNextToken().Word.Value is Tokens.Keyword.Store)
                    {
                        code.MoveNext();
                        programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, index, isDynamicIndex));
                        programPieces.Add(Instructions.StoreTyped(token, memoryAllocation.Typing));
                    }
                    else
                    {
                        programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, index, isDynamicIndex));
                    }
                }
                else if (code.HasNextToken() && code.PeekNextToken().Word.Value is Tokens.Keyword.Load)
                {
                    code.MoveNext();
                    programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, 0, false));
                    programPieces.Add(Instructions.LoadTyped(token, memoryAllocation.Typing));
                }
                else if (code.HasNextToken() && code.PeekNextToken().Word.Value is Tokens.Keyword.Store)
                {
                    code.MoveNext();
                    programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, 0, false));
                    programPieces.Add(Instructions.StoreTyped(token, memoryAllocation.Typing));
                }
                else
                {
                    programPieces.Add(Instructions.PushMemoryPointer(token, memoryAllocation, 0, false));
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
                if (definitions.TryGetStructure(name, out structureDefinition))
                {
                    var structure = structureDefinition.Parse(definitions);
                    if (!structure.Fields.TryGetValue(fieldName, out var field))
                    {
                        throw new Exception($"Unknown member {fieldName}.");
                    }
                    programPieces.Add(Instructions.StructFieldOffset(token, field.Offset));
                    continue;
                }
                else if (definitions.TryGetMemory(name, out var memory))
                {
                    programPieces.Add(Instructions.PushMemoryPointer(token, memory, 0, fieldName, false));
                    continue;
                }
                else if (definitions.TryGetPin(name, out var pinnedItem))
                {
                    programPieces.Add(Instructions.PushFieldOfPinnedStackItem(pinnedItem.Current, fieldName));
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
                if (!code.HasNextToken())
                {
                    throw new Exception($"Expected type after {token}, but got nothing.");
                }
                var typeToken = code.MoveNext();
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

    private static void ParseTypedAllocation(SourceCode code, Definitions definitions, Token token, TypingType typing)
    {
        if (!code.HasNextToken())
        {
            throw new Exception($"Expected identifier after {token}, but got nothing.");
        }

        var next = code.MoveNext();
        var identifier = next;
        var count = 1;
        if (next.Word.Value is "[")
        {
            var countToken = code.MoveNext();
            if (!code.HasNextToken())
            {
                throw new Exception($"Expected count after {countToken}, but got nothing.");
            }
            count = int.Parse(countToken.Word.Value);
            if (!code.HasNextToken())
            {
                throw new Exception($"Expected `]` after {countToken}, but got nothing.");
            }
            code.MoveNext();

            identifier = code.MoveNext();
        }

        definitions.AddMemory(identifier, typing, count);
    }

    private static Loop ParseLoop(SourceCode code, Definitions definitions)
    {
        var token = code.CurrentToken();
        if (!code.HasNextToken())
        {
            throw new Exception($"Expected loop condition, but got nothing @ {token}");
        }
        var iteratorLabel = code.MoveNext();

        var iterator = definitions.PinStackItem(iteratorLabel, Typing.Create(Primitives.Runtime));

        if (!code.HasNextToken())
        {
            throw new Exception($"Expected condition after loop iterator, but got nothing @ {iteratorLabel}");
        }
        var condition = ParseProgram(code, definitions, ":");
        var endToken = code.MoveNext();
        if (endToken.Word.Value is not ":")
        {
            throw new Exception($"Expected `:` after loop condition, but got {endToken}");
        }
        var body = ParseProgram(code, definitions, Tokens.BlockEnd);
        var endBodyToken = code.MoveNext();
        if (endBodyToken.Word.Value is not Tokens.BlockEnd)
        {
            throw new Exception($"Expected `;` after loop body, but got {endBodyToken}");
        }

        var iteratorTop = iterator.Current;
        iterator.Unpin();
        return new(token, iteratorTop, condition, body);
    }


    private static Branching ParseBranches(SourceCode code, Definitions definitions)
    {
        List<IProgramPiece>? yesBranch = null;
        List<IProgramPiece>? noBranch = null;
        var token = code.CurrentToken();
        if (!code.HasNextToken())
        {
            throw new Exception($"Expected yes: or no: after {token}, but got nothing.");
        }
        var firstBranch1Token = code.MoveNext();
        if (firstBranch1Token.Word.Value is not "yes" and not "no")
        {
            throw new Exception($"Expected `yes:` or `no:` after ?, but got {firstBranch1Token}");
        }
        if (!code.HasNextToken())
        {
            throw new Exception($"Expected `:` after {firstBranch1Token}, but got nothing.");
        }
        var firstBranchBlockStartToken = code.MoveNext();
        if (firstBranchBlockStartToken.Word.Value is not ":")
        {
            throw new Exception($"Expected `:` after {firstBranch1Token}, but got {firstBranchBlockStartToken}");
        }
        var branch1Program = ParseProgram(code, definitions, ";");
        var branchEndToken = code.MoveNext();
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

        if (!code.HasRemainingTokens(3) || code.PeekNextToken().Word.Value != expectedBranch2Token || code.PeekNthToken(2).Word.Value is not ":")
        {
            return new(token, yesBranch, noBranch);
        }

        var firstBranch2Token = code.MoveNext();
        code.MoveNext(); // :

        var branch2Program = ParseProgram(code, definitions, ";");
        var endBranch2Token = code.MoveNext();
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

    public static List<ParameterType> ParseParameters(Definitions definitions, SourceCode code)
    {
        if (!code.HasNextToken())
        {
            return [];
        }

        var parameters = new List<ParameterType>();

        while (code.HasNextToken())
        {
            var token = code.MoveNext();
            if (!TryParseTyping(definitions, token, out var typing))
            {
                throw new Exception($"Expected type, but got {token}.");
            }

            if (!code.HasNextToken() || TryParseTyping(definitions, code.PeekNextToken(), out _))
            {
                parameters.Add(Parameter.Create(typing));
                continue;
            }

            var nameToken = code.MoveNext();

            parameters.Add(Parameter.Create(nameToken, typing));
        }

        return parameters;
    }

    public static bool TryParseTyping(Definitions definitions, Token token, out TypingType typing)
    {
        typing = token.Word.Value switch
        {
            _ when Tokens.Primitive.TryParsePrimitive(token.Word.Value, out var dataType) => Typing.Create(dataType),
            _ when token.Word.Value == Structure.ZeroTerminatedString.Name => Typing.Create(Structure.ZeroTerminatedString),
            _ when token.Word.Value == Structure.String.Name => Typing.Create(Structure.String),
            _ when definitions.TryGetStructure(token, out var structure) => Typing.Create(structure.Parse(definitions)),
            _ => null!,
        };
        return typing is not null;
    }
}
