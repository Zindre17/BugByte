using static BugByte.Lexer;

namespace BugByte;

internal static class Parser
{
    internal static Block GroupBlock(Token? last, Queue<Token> tokens, Dictionary<string, Block> functions, Dictionary<string, Constant> constants, Dictionary<string, Structure> structures, Dictionary<string, Token> inclusions, string? expectedClosingTag = null)
    {
        var block = new Block(new(), new(), functions, constants, structures);
        if (tokens.Count is 0)
        {
            if (last is null)
            {
                throw new Exception("Empty program.");
            }
            throw new Exception($"Empty block after {last}.");
        }
        while (tokens.Count > 0)
        {
            var token = tokens.Dequeue();
            if (expectedClosingTag is not null && (token.Value == expectedClosingTag))
            {
                if (block.Tokens.Count is 0)
                {
                    throw new Exception($"Empty block after {last}");
                }
                block.Tokens.Enqueue(token);
                return block;
            }
            else if (token.Value is "?")
            {
                block.Tokens.Enqueue(token);
                var missingBranch = "";
                if (tokens.Count is 0)
                {
                    throw new Exception($"Missing branch after {token}");
                }
                if (tokens.Peek().Value is "yes" or "no" && tokens.Count > 0)
                {
                    missingBranch = tokens.Peek().Value is "yes" ? "no" : "yes";
                    block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ";"));
                }
                if (tokens.Count is 0)
                {
                    continue;
                }
                if (tokens.Peek().Value == missingBranch && tokens.Count > 0)
                {
                    block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ";"));
                }
            }
            else if (token.Value is "while")
            {
                block.Tokens.Enqueue(token);
                if (tokens.Count is 0)
                {
                    throw new Exception($"Expected at least one token after `while`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                }
                block.Tokens.Enqueue(tokens.Dequeue());
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ":"));
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ";"));
            }
            else if (token.Value is "using")
            {
                block.Tokens.Enqueue(token);
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ":"));
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ";"));
            }
            else if (token.Value.EndsWith("()"))
            {
                var functionName = token.Value[..^2];
                if (tokens.Count is 0 || tokens.Dequeue().Value is not ":")
                {
                    throw new Exception($"Missing ':' after function declaration {functionName}.");
                }
                var functionBlock = GroupBlock(token, tokens, block.Functions, block.Constants, block.Structures, inclusions, ";");
                if (!block.Functions.TryAdd(functionName, functionBlock))
                {
                    throw new Exception($"Duplicate function {functionName}.");
                }
            }
            else if (token.Value is "struct")
            {
                var structName = tokens.Dequeue();
                var fields = new Dictionary<string, StructureField>();

                if (IsKeyword(structName.Value, out var keyword))
                {
                    throw new Exception($"Expected identifier, but got an existing keyword {keyword}.");
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
                    if (member.Value is ";")
                    {
                        break;
                    }
                    if (IsKeyword(member.Value, out keyword))
                    {
                        throw new Exception($"Expected identifier, but got an existing keyword {keyword}.");
                    }

                    var sizeToken = tokens.Dequeue();
                    if (sizeToken.Value is ";")
                    {
                        break;
                    }

                    if (!int.TryParse(sizeToken.Value, out var size))
                    {
                        throw new Exception($"Expected integer, but got {sizeToken}.");
                    }

                    if (tokens.Count is 0)
                    {
                        throw new Exception($"Expected identifier, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                    }

                    fields.Add(member.Value, new(offset, size, member.Value));
                    offset += size;

                    if (tokens.Count is 0)
                    {
                        throw new Exception($"Unclosed struct definition @ {token.Filename}:{token.Line}:{token.Column}");
                    }
                }
                var structure = new Structure(structName.Value, fields);
                block.Structures.Add(structure.Name, structure);
            }
            else if (token.Value is "aka")
            {
                if (tokens.Count < 2)
                {
                    throw new Exception($"Expected at least two tokens after `aka`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var name = tokens.Dequeue();
                if (IsKeyword(name.Value, out var keyword))
                {
                    throw new Exception($"Expected identifier, but got an existing keyword {keyword}.");
                }
                var constant = tokens.Dequeue();
                if (int.TryParse(constant.Value, out var constInt))
                {
                    block.Constants.Add(name.Value, new(DataType.Number, Number: constInt));
                }
                else if (IsString(constant, out var constString))
                {
                    block.Constants.Add(name.Value, new(DataType.String, Text: constString));
                }
                else if (IsZeroTerminatedString(constant, out var constZeroString))
                {
                    block.Constants.Add(name.Value, new(DataType.ZeroTerminatedString, Text: constZeroString));
                }
                else
                {
                    throw new Exception($"Expected number after `aka`, but got {constant} @ {constant.Filename}:{constant.Line}:{constant.Column}");
                }
            }
            else if (token.Value is "include")
            {
                var includePath = tokens.Dequeue();
                if (!IsString(includePath, out var path))
                {
                    throw new Exception($"Expected string after include, but got {includePath} @ {includePath.Filename}:{includePath.Line}:{includePath.Column}");
                }
                var fullPath = Path.GetFullPath(path);
                Console.WriteLine($"Including {path}");
                if (inclusions.TryGetValue(fullPath, out var existingInclude))
                {
                    throw new Exception($"Cannot include {path} because it is already included at {existingInclude}.");
                }
                inclusions.Add(fullPath, includePath);
                var words = LexFile(path);
                var blocks = GroupBlock(null, words, new(), new(), new(), inclusions);

                foreach (var (key, function) in blocks.Functions)
                {
                    if (block.Functions.ContainsKey(key))
                    {
                        continue;
                    }
                    block.Functions.Add(key, function);
                }
                foreach (var (key, structure) in blocks.Structures)
                {
                    if (block.Structures.ContainsKey(key))
                    {
                        continue;
                    }
                    block.Structures.Add(key, structure);
                }
                foreach (var (key, constant) in blocks.Constants)
                {
                    if (block.Constants.ContainsKey(key))
                    {
                        continue;
                    }
                    block.Constants.Add(key, constant);
                }
            }
            else
            {
                block.Tokens.Enqueue(token);
            }
        }
        if (expectedClosingTag is not null)
        {
            var openingToken = block.Tokens.Peek();
            throw new Exception($"Unclosed block at @ {openingToken.Filename}:{openingToken.Line}:{openingToken.Column}");
        }
        return block;
    }

    // TODO: Allocate memory relative to the current scope from a pool
    // TODO: Deallocate memory at the end of scope
    internal static ParsedProgram ParseProgram(Block block, Dictionary<string, Token> memories, Dictionary<string, int>? pinnedStackItems = null, string? inlineLabelModifier = null)
    {
        var inclusions = new Dictionary<string, Token>();
        var operations = new List<Operation>();
        var tokens = block.Tokens;
        var program = new ParsedProgram(operations, new());
        var modifier = inlineLabelModifier ?? "";
        while (tokens.Count > 0)
        {
            var token = tokens.Dequeue();
            if (pinnedStackItems is not null && pinnedStackItems.TryGetValue(token.Value, out _))
            {
                operations.Add(new Operation(OperationType.PushPinnedStackItem, token, new Meta(Text: token.Value)));
            }
            else if (block.Functions.TryGetValue(token.Value, out var functionBlock))
            {
                var parsedFunction = ParseProgram(functionBlock.Copy(), new(), inlineLabelModifier: $"{modifier}_{token.Line}_{token.Column}");
                program.NestedPrograms.Enqueue(parsedFunction);
                operations.Add(new Operation(OperationType.Inline, token));
            }
            else if (memories.ContainsKey(token.Value))
            {
                operations.Add(new Operation(OperationType.PushMemory, token, new Meta(Text: token.Value + modifier)));
            }
            else if (block.Constants.TryGetValue(token.Value, out var constant1))
            {
                if (constant1.Type is DataType.Number)
                {
                    operations.Add(new Operation(OperationType.PushNumber, token, new Meta(Number: constant1.Number)));
                }
                else if (constant1.Type is DataType.ZeroTerminatedString)
                {
                    operations.Add(new Operation(OperationType.PushZeroString, token, new Meta(Text: constant1.Text)));
                }
                else if (constant1.Type is DataType.String)
                {
                    operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: constant1.Text)));
                }
                else
                {
                    throw new Exception($"Unknown constant type {constant1.Type} from {token}.");
                }
            }
            else if (block.Structures.TryGetValue(token.Value.Split(".", 2)[0], out var structType))
            {
                if (!structType.Fields.TryGetValue(token.Value.Split(".", 2)[1], out var structField))
                {
                    throw new Exception($"Unknown field {token.Value.Split(".", 2)[1]} in structure {structType.Name} from {token}.");
                }
                operations.Add(new Operation(OperationType.PushNumber, token, new Meta(Number: structField.Offset)));
                operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.Add)));
            }
            else if (int.TryParse(token.Value, out var value))
            {
                operations.Add(new Operation(OperationType.PushNumber, token, new Meta(Number: value)));
            }
            else if (token.Value is "+")
            {
                ParseOperator(token, Operator.Add);
            }
            else if (token.Value is "-")
            {
                ParseOperator(token, Operator.Subtract);
            }
            else if (token.Value is "*")
            {
                ParseOperator(token, Operator.Multiply);
            }
            else if (token.Value is "/")
            {
                ParseOperator(token, Operator.Divide);
            }
            else if (token.Value is "%")
            {
                ParseOperator(token, Operator.Modulo);
            }
            else if (token.Value is "^")
            {
                ParseOperator(token, Operator.Xor);
            }
            else if (token.Value is "|")
            {
                ParseOperator(token, Operator.Or);
            }
            else if (token.Value is "&")
            {
                ParseOperator(token, Operator.And);
            }
            else if (token.Value is "=")
            {
                ParseOperator(token, Operator.Equal);
            }
            else if (token.Value is "<<")
            {
                ParseOperator(token, Operator.LeftShift);
            }
            else if (token.Value is ">>")
            {
                ParseOperator(token, Operator.RightShift);
            }
            else if (token.Value is "==")
            {
                operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.StringEqual)));
            }
            else if (token.Value is "!==")
            {
                operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.StringEqual)));
                operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.Not)));
            }
            else if (token.Value is "!=")
            {
                ParseOperator(token, Operator.NotEqual);
            }
            else if (token.Value is "<")
            {
                ParseOperator(token, Operator.LessThan);
            }
            else if (token.Value is "<=")
            {
                ParseOperator(token, Operator.LessThanOrEqual);
            }
            else if (token.Value is ">")
            {
                ParseOperator(token, Operator.GreaterThan);
            }
            else if (token.Value is ">=")
            {
                ParseOperator(token, Operator.GreaterThanOrEqual);
            }
            else if (token.Value is "dup")
            {
                operations.Add(new Operation(OperationType.PushDuplicate, token));
            }
            else if (token.Value is "drop")
            {
                operations.Add(new Operation(OperationType.Drop, token));
            }
            else if (token.Value is "over")
            {
                operations.Add(new Operation(OperationType.Over, token));
            }
            else if (token.Value is "swap")
            {
                operations.Add(new Operation(OperationType.Swap, token));
            }
            else if (token.Value is "print")
            {
                operations.Add(new Operation(OperationType.Print, token));
            }
            else if (token.Value is "prints")
            {
                operations.Add(new Operation(OperationType.PrintString, token));
            }
            else if (token.Value is "alloc")
            {
                var nextToken = GetNextToken("Expected `[` after `alloc`, but got nothing.");
                if (nextToken.Value is not "[")
                {
                    throw new Exception($"Expected `[` after `alloc`, but got {nextToken}.");
                }
                nextToken = GetNextToken($"Expected size after `[`, but got nothing.");
                int size;
                if (int.TryParse(nextToken.Value, out var number))
                {
                    size = number;
                }
                else if (block.Structures.TryGetValue(nextToken.Value, out var structure))
                {
                    size = structure.Size;
                }
                else
                {
                    throw new Exception($"Expected a number or structure type after `alloc` but got {nextToken}");
                }
                var endToken = GetNextToken($"Expected closing `]`, but got nothing.");
                if (endToken.Value is not "]")
                {
                    throw new Exception($"Unclosed `alloc` size definition @ {token.Filename}:{token.Line}:{token.Column}");
                }

                var name = GetNextToken($"Expected name after `alloc[{nextToken.Value}]` but got nothing.");
                if (IsKeyword(name.Value, out var keyword))
                {
                    throw new Exception($"`{name.Value}` is a keyword ({keyword}) and cannot be used as a memory name @ {name.Filename}:{name.Line}:{name.Column}");
                }
                if (!memories.TryAdd(name.Value, name))
                {
                    throw new Exception($"`{name.Value}` is already allocated at {memories[name.Value]}");
                }

                operations.Add(new Operation(OperationType.AllocateMemory, token, new Meta(Number: size, Text: name.Value + modifier)));
            }
            else if (token.Value is "store")
            {
                operations.Add(new Operation(OperationType.StoreMemory, token));
            }
            else if (token.Value is "load")
            {
                operations.Add(new Operation(OperationType.LoadMemory, token));
            }
            else if (token.Value is "load-byte")
            {
                operations.Add(new Operation(OperationType.LoadByte, token));
            }
            else if (token.Value is "(ptr)")
            {
                operations.Add(new Operation(OperationType.Cast, token, new Meta(Type: DataType.Pointer)));
            }
            else if (token.Value is "syscall0")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 0)));
            }
            else if (token.Value is "syscall1")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 1)));
            }
            else if (token.Value is "syscall2")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 2)));
            }
            else if (token.Value is "syscall3")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 3)));
            }
            else if (token.Value is "syscall4")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 4)));
            }
            else if (token.Value is "syscall5")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 5)));
            }
            else if (token.Value is "syscall6")
            {
                operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 6)));
            }
            else if (IsZeroTerminatedString(token, out var zerostr))
            {
                operations.Add(new Operation(OperationType.PushZeroString, token, new Meta(Text: zerostr)));
            }
            else if (IsString(token, out var str))
            {
                operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: str)));
            }
            else if (token.Value is "yes")
            {
                operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: true)));
            }
            else if (token.Value is "no")
            {
                operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: false)));
            }
            else if (token.Value is ":")
            {

            }
            else if (token.Value is ";")
            {
                if (tokens.Count > 0)
                {
                    throw new Exception($"Expected nothing after `;`, but got `{tokens.Peek().Value}` @ {tokens.Peek().Filename}:{tokens.Peek().Line}:{tokens.Peek().Column}");
                }
            }
            else if (token.Value is "?")
            {
                if (block.NestedBlocks.Count is 0)
                {
                    throw new Exception($"Expected at least one branch block after ?, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var branch1 = block.NestedBlocks.Dequeue();
                var firstBranch1Token = branch1.Tokens.Dequeue()
                    ?? throw new Exception($"Expected yes: or no: after ?, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                if (firstBranch1Token.Value is not "yes" and not "no")
                {
                    throw new Exception($"Expected yes: or no: after ?, but got `{firstBranch1Token.Value}` @ {firstBranch1Token.Filename}:{firstBranch1Token.Line}:{firstBranch1Token.Column}");
                }

                var branch1Program = ParseProgram(branch1, memories, pinnedStackItems, modifier);

                var endLabel = $"end_if_{token.Line}_{token.Column}{modifier}";
                var expectedBranch2Token = firstBranch1Token.Value is "yes" ? "no" : "yes";

                if (block.NestedBlocks.Count is 0 || block.NestedBlocks.Peek().Tokens.Peek().Value != expectedBranch2Token)
                {
                    operations.Add(new Operation(firstBranch1Token.Value is "yes" ? OperationType.JumpIfZero : OperationType.JumpIfNotZero, token, new Meta(Text: endLabel)));
                    operations.Add(new Operation(OperationType.Branch, token, new Meta(Number: 1)));
                    program.NestedPrograms.Enqueue(branch1Program);
                    operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
                    continue;
                }

                var branch2 = block.NestedBlocks.Dequeue();
                var firstBranch2Token = branch2.Tokens.Dequeue();
                var branch2Program = ParseProgram(branch2, memories, pinnedStackItems, modifier);

                ParsedProgram yesBlock;
                ParsedProgram noBlock;
                Token yesToken;
                Token noToken;
                if (firstBranch1Token.Value is "yes")
                {
                    yesBlock = branch1Program;
                    noBlock = branch2Program;
                    yesToken = firstBranch1Token;
                    noToken = firstBranch2Token;
                }
                else
                {
                    yesBlock = branch2Program;
                    noBlock = branch1Program;
                    yesToken = firstBranch2Token;
                    noToken = firstBranch1Token;
                }
                var startYesBlockLabel = $"start_yes_branch_{yesToken.Line}_{yesToken.Column}{modifier}";
                operations.Add(new Operation(OperationType.JumpIfNotZero, token, new Meta(Text: startYesBlockLabel)));
                operations.Add(new Operation(OperationType.Branch, token, new Meta(Number: 2)));

                noBlock.Operations.Add(new Operation(OperationType.Jump, token, new Meta(Text: endLabel)));
                program.NestedPrograms.Enqueue(noBlock);

                yesBlock.Operations.Insert(0, new Operation(OperationType.Label, token, new Meta(Text: startYesBlockLabel)));
                program.NestedPrograms.Enqueue(yesBlock);

                operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
            }
            else if (token.Value is "while")
            {
                if (block.NestedBlocks.Count is 0)
                {
                    throw new Exception($"Expected identifier after `while`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var label = tokens.Dequeue();
                if (IsKeyword(label.Value, out var kwd))
                {
                    throw new Exception($"Expected identifier after `while`, but got an existing keyword ({kwd}) {token}.");
                }

                pinnedStackItems ??= new Dictionary<string, int>();
                if (pinnedStackItems.TryGetValue(label.Value, out var current))
                {
                    pinnedStackItems[label.Value] = current + 1;
                }
                else
                {
                    pinnedStackItems[label.Value] = 1;
                }

                if (block.NestedBlocks.Count < 2)
                {
                    throw new Exception($"Expected at least two blocks after while, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var conditionBlock = block.NestedBlocks.Dequeue();
                var whileBlock = block.NestedBlocks.Dequeue();

                var whileStartLabel = $"while_{token.Line}_{token.Column}{modifier}";
                var whileEndLabel = $"end_while_{token.Line}_{token.Column}{modifier}";

                operations.Add(new Operation(OperationType.PinStackElement, label));

                operations.Add(new Operation(OperationType.Label, token, new Meta(Text: whileStartLabel)));
                operations.Add(new Operation(OperationType.Loop, token));

                var conditionProgram = ParseProgram(conditionBlock, memories, pinnedStackItems, modifier);
                conditionProgram.Operations.Insert(0, new Operation(OperationType.PushPinnedStackItem, label));
                conditionProgram.Operations.Add(new Operation(OperationType.JumpIfZero, token, new Meta(Text: whileEndLabel)));
                program.NestedPrograms.Enqueue(conditionProgram);

                var whileProgram = ParseProgram(whileBlock, memories, pinnedStackItems, modifier);
                whileProgram.Operations.Add(new Operation(OperationType.UpdatePinnedStackElement, label));
                program.NestedPrograms.Enqueue(whileProgram);
                operations.Add(new Operation(OperationType.Jump, token, new Meta(Text: whileStartLabel)));
                operations.Add(new Operation(OperationType.Label, token, new Meta(Text: whileEndLabel)));
                operations.Add(new Operation(OperationType.PushPinnedStackItem, label));
                operations.Add(new Operation(OperationType.UnpinStackElement, label));
                if (pinnedStackItems[label.Value] is 1)
                {
                    pinnedStackItems.Remove(label.Value);
                }
                else
                {
                    pinnedStackItems[label.Value] -= 1;
                }
            }
            else if (token.Value is "using")
            {
                if (block.NestedBlocks.Count < 2)
                {
                    throw new Exception($"Expected at least two blocks after using, but got {block.NestedBlocks.Count} @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var assignmentBlock = block.NestedBlocks.Dequeue();
                if (assignmentBlock.Tokens.Count is 0)
                {
                    throw new Exception($"Expected at least one assignment, but got none @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var assignments = new List<Token>();
                while (assignmentBlock.Tokens.Count > 0)
                {
                    var assignment = assignmentBlock.Tokens.Dequeue();
                    if (assignment.Value is ":")
                    {
                        break;
                    }
                    if (IsKeyword(assignment.Value, out var keyword))
                    {
                        throw new Exception($"Expected identifier, but got an existing keyword ({keyword}) {token}.");
                    }
                    assignments.Add(assignment);
                }
                if (assignments.Count is 0)
                {
                    throw new Exception($"Expected at least one assignment, but got none @ {token.Filename}:{token.Line}:{token.Column}");
                }

                pinnedStackItems ??= new Dictionary<string, int>();
                for (var i = assignments.Count - 1; i >= 0; i--)
                {
                    if (pinnedStackItems.ContainsKey(assignments[i].Value))
                    {
                        pinnedStackItems[assignments[i].Value] += 1;
                    }
                    else
                    {
                        pinnedStackItems[assignments[i].Value] = 1;
                    };
                    operations.Add(new Operation(OperationType.PinStackElement, assignments[i]));
                }
                operations.Add(new Operation(OperationType.UsingBlock, token));
                var consumingBlock = block.NestedBlocks.Dequeue();
                var consumingProgram = ParseProgram(consumingBlock, memories, pinnedStackItems, modifier);
                program.NestedPrograms.Enqueue(consumingProgram);

                foreach (var assignment in assignments)
                {
                    operations.Add(new Operation(OperationType.UnpinStackElement, assignment));
                    if (pinnedStackItems[assignment.Value] is 1)
                    {
                        pinnedStackItems.Remove(assignment.Value);
                    }
                    else
                    {
                        pinnedStackItems[assignment.Value] -= 1;
                    }
                }
            }
            else if (token.Value is "include")
            {
                throw new Exception("Includes should have been handled by now.");
            }
            else if (token.Value is "struct")
            {
                throw new Exception("Structs should have been parsed by now.");
            }
            else if (token.Value is "inspect")
            {
                operations.Add(new Operation(OperationType.Inspect, token));
            }
            else if (token.Value is "aka")
            {
                throw new Exception("Constants should have been parsed by now.");
            }
            else if (token.Value is "exit")
            {
                operations.Add(new Operation(OperationType.Exit, token));
            }
            else
            {
                throw new Exception($"Unknown token {token}");
            }
        }

        return program;

        void ParseOperator(Token token, Operator operatorType)
        {
            operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: operatorType)));
        }

        Token GetNextToken(string failedMessage)
        {
            if (tokens.Count == 0)
            {
                throw new Exception(failedMessage);
            }
            return tokens?.Dequeue()
                ?? throw new Exception("Unexpected block start");
        }
    }

    internal static bool IsString(Token token, out string value)
    {
        if (token.Value.StartsWith("\"") && token.Value.EndsWith("\""))
        {
            value = token.Value[1..^1];
            return true;
        }
        value = "";
        return false;
    }

    internal static bool IsZeroTerminatedString(Token token, out string value)
    {
        if (token.Value.StartsWith("0\"") && token.Value.EndsWith("\""))
        {
            value = token.Value[2..^1] + '\0';
            return true;
        }
        value = "";
        return false;
    }

    internal static bool IsKeyword(string word, out Keyword keyword)
    {
        keyword = word switch
        {
            "dup" => Keyword.Duplicate,
            "drop" => Keyword.Drop,
            "over" => Keyword.Over,
            "swap" => Keyword.Swap,
            "print" => Keyword.Print,
            "prints" => Keyword.PrintString,
            "+" => Keyword.Addition,
            "-" => Keyword.Subtraction,
            "*" => Keyword.Multiplication,
            "/" => Keyword.Division,
            "%" => Keyword.Modulo,
            "&" => Keyword.And,
            "|" => Keyword.Or,
            "^" => Keyword.Xor,
            "==" => Keyword.StringEqual,
            "!==" => Keyword.StringNotEqual,
            "=" => Keyword.Equal,
            "!=" => Keyword.NotEqual,
            "<" => Keyword.LessThan,
            "<=" => Keyword.LessThanOrEqual,
            ">" => Keyword.GreaterThan,
            ">=" => Keyword.GreaterThanOrEqual,
            "yes" => Keyword.Yes,
            "no" => Keyword.No,
            "?" => Keyword.ConditionalExpression,
            "yes:" => Keyword.YesBranch,
            "no:" => Keyword.NoBranch,
            "include" => Keyword.Include,
            "using" => Keyword.Using,
            "while" => Keyword.While,
            "alloc[" => Keyword.Allocate,
            "load" => Keyword.Load,
            "store" => Keyword.Store,
            "load-byte" => Keyword.LoadByte,
            "(ptr)" => Keyword.CastToPointer,
            "syscall0" => Keyword.Syscall0,
            "syscall1" => Keyword.Syscall1,
            "syscall2" => Keyword.Syscall2,
            "syscall3" => Keyword.Syscall3,
            "syscall4" => Keyword.Syscall4,
            "syscall5" => Keyword.Syscall5,
            "syscall6" => Keyword.Syscall6,
            _ => Keyword.Unknown,
        };
        return keyword != Keyword.Unknown;
    }
}

internal record Block(Queue<Token> Tokens, Queue<Block> NestedBlocks, Dictionary<string, Block> Functions, Dictionary<string, Constant> Constants, Dictionary<string, Structure> Structures)
{
    internal Block Copy()
    {
        return new Block(
            new Queue<Token>(new Queue<Token>(Tokens)),
            new Queue<Block>(new Queue<Block>(NestedBlocks.Select(b => b.Copy()))),
            new Dictionary<string, Block>(Functions),
            new Dictionary<string, Constant>(Constants),
            new Dictionary<string, Structure>(Structures));
    }
}

internal record Constant(DataType Type, int? Number = null, string? Text = null);

internal record Meta(int? Number = null, string? Text = null, Operator? Operator = null, bool? Bool = null, DataType? Type = null);

internal record Operation(OperationType Type, Token Token, Meta? Data = null);

internal record ParsedProgram(List<Operation> Operations, Queue<ParsedProgram> NestedPrograms);

internal record Structure(string Name, Dictionary<string, StructureField> Fields)
{
    internal int Size => Fields.Sum(f => f.Value.Size);
}

internal record StructureField(int Offset, int Size, string Name);


internal enum Keyword
{
    Duplicate,
    Drop,
    Over,
    Swap,
    Print,
    PrintString,
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Modulo,
    And,
    Or,
    Xor,
    StringEqual,
    StringNotEqual,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Yes,
    No,
    ConditionalExpression,
    YesBranch,
    NoBranch,
    Include,
    Using,
    While,
    Allocate,
    Load,
    Store,
    LoadByte,
    CastToPointer,
    Syscall0,
    Syscall1,
    Syscall2,
    Syscall3,
    Syscall4,
    Syscall5,
    Syscall6,
    Unknown,
}

enum Operator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,

    Not,

    And,
    Or,
    Xor,

    LeftShift,
    RightShift,

    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    StringEqual,
}

enum OperationType
{
    Print,
    PrintString,
    JumpIfZero,
    JumpIfNotZero,
    Label,
    Jump,
    Drop,
    Over,
    Swap,
    PushNumber,
    PushString,
    PushZeroString,
    PushBool,
    PushDuplicate,
    PushPinnedStackItem,
    Operator,
    UsingBlock,
    PinStackElement,
    UnpinStackElement,
    UpdatePinnedStackElement,
    Syscall,
    AllocateMemory,
    PushMemory,
    StoreMemory,
    LoadMemory,
    LoadByte,
    Cast,
    Branch,
    Loop,
    Inline,
    Inspect,
    Exit,
}

enum DataType
{
    Number,
    Pointer,
    String,
    ZeroTerminatedString,
}
