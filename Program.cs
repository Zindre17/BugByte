using System.Diagnostics;

if (args.Length is 0)
{
    Console.WriteLine("Please provide a file name to lex");
    return;
}
if (args.Length > 1)
{
    Console.WriteLine("Please provide only one file name");
    return;
}

var fileName = args[0];

var words = LexProgram(fileName);

try
{
    var program = ParseProgram(words);
    GenerateAsembly(program, fileName);
}
catch (Exception e)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine("Failed to compile.");
    Console.ResetColor();
    Environment.Exit(1);
}

Console.WriteLine("Compiled successfully.");

var fileNameWithoutExtension = fileName.Split(".")[0];

if (!RunExternalCommand("fasm", $"{fileNameWithoutExtension}.asm {fileNameWithoutExtension}"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Failed to assemble.");
    Console.ResetColor();
    Environment.Exit(1);
}

if (!RunExternalCommand("chmod", $"+x {fileNameWithoutExtension}"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Failed to make executable.");
    Console.ResetColor();
    Environment.Exit(1);
}

RunExternalCommand($"./{fileNameWithoutExtension}", "", false);

static bool RunExternalCommand(string command, string arguments, bool printInfo = true)
{
    var startInfo = new ProcessStartInfo()
    {
        FileName = command,
        Arguments = arguments,
    };
    try
    {
        var process = Process.Start(startInfo);
        if (process is null)
        {
            if (printInfo)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Failed to start external process.");
                Console.ResetColor();
            }
            return false;
        }
        if (printInfo)
        {
            Console.WriteLine($"Running command: {command} {arguments}");
        }
        process.WaitForExit();
        if (process.ExitCode is not 0)
        {
            if (printInfo)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Command failed. Error({process.ExitCode}): {process.StandardError.ReadToEnd()}");
                Console.ResetColor();
            }
            return false;
        }
        return true;
    }
    catch (System.ComponentModel.Win32Exception e)
    {
        if (e.HResult is -2147467259)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Command not found: {command}. Make sure you have all the dependencies installed.");
            Console.ResetColor();
            return false;
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"{e.GetType()}({e.HResult}): {e.Message}");
        Console.ResetColor();
        return false;
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"{e.GetType()}({e.HResult}): {e.Message}");
        Console.ResetColor();
        return false;
    }
}

static void GenerateAsembly(ParsedProgram program, string filename)
{
    var assembly = new List<string>
    {
        "format ELF64 executable 3",
        "segment readable executable",
        "entry start",
        
        // TODO: Implement this yourself / understand what it does
        "print:"                               ,
        "  mov     r9, -3689348814741910323" ,
        "  sub     rsp, 40"                  ,
        "  mov     BYTE [rsp+31], 10"        ,
        "  lea     rcx, [rsp+30]"            ,
        ".L2:"                                 ,
        "  mov     rax, rdi"                 ,
        "  lea     r8, [rsp+32]"             ,
        "  mul     r9"                       ,
        "  mov     rax, rdi"                 ,
        "  sub     r8, rcx"                  ,
        "  shr     rdx, 3"                   ,
        "  lea     rsi, [rdx+rdx*4]"         ,
        "  add     rsi, rsi"                 ,
        "  sub     rax, rsi"                 ,
        "  add     eax, 48"                  ,
        "  mov     BYTE [rcx], al"           ,
        "  mov     rax, rdi"                 ,
        "  mov     rdi, rdx"                 ,
        "  mov     rdx, rcx"                 ,
        "  sub     rcx, 1"                   ,
        "  cmp     rax, 9"                   ,
        "  ja      .L2"                      ,
        "  lea     rax, [rsp+32]"            ,
        "  mov     edi, 1"                   ,
        "  sub     rdx, rax"                 ,
        "  xor     eax, eax"                 ,
        "  lea     rsi, [rsp+32+rdx]"        ,
        "  mov     rdx, r8"                  ,
        "  mov     rax, 1"                   ,
        "  syscall"                          ,
        "  add     rsp, 40"                  ,
        "  ret"                              ,

        "start:"
    };

    var stringLiterals = new List<string>();
    foreach (var operation in program.Operations)
    {
        if (operation.Type is OperationType.PushNumber)
        {
            var number = operation.Data?.Number
                ?? throw new Exception($"Operation was of type number but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            assembly.Add($";-- number --");
            assembly.Add($"  mov rax, {number}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.PushString)
        {
            var text = operation.Data?.Text
                ?? throw new Exception($"Operation was of type string but has no value. Probably a bug in the parser.");

            assembly.Add($";-- string --");
            assembly.Add($"  mov rax, {text.Length - text.Count(c => c == '\\')}");
            assembly.Add($"  push rax");
            assembly.Add($"  push string_{stringLiterals.Count}");

            stringLiterals.Add(text);
        }
        else if (operation.Type is OperationType.PushBool)
        {
            var boolean = operation.Data?.Bool
                ?? throw new Exception($"Operation was of type boolean but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            assembly.Add(";-- boolean --");
            assembly.Add($"  mov rax, {(boolean ? 1 : 0)}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.PushDuplicate)
        {
            assembly.Add(";-- duplicate --");
            assembly.Add($"  pop rax");
            assembly.Add($"  push rax");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.Operator)
        {
            var op = operation.Data?.Operator
                ?? throw new Exception($"Operation was of type operator but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            var number = operation.Data.Number
                ?? throw new Exception($"Operation was of type operator but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            if (op is Operator.Add)
            {
                assembly.Add(";-- add --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  add rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Subtract)
            {
                assembly.Add(";-- subtract --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  sub rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Multiply)
            {
                assembly.Add(";-- multiply --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  mul rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Divide)
            {
                assembly.Add(";-- divide --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  div rbx");
                assembly.Add($"  push rax");
                // NOTE: this ignores the remainder
            }
            else if (op is Operator.Equal)
            {
                assembly.Add(";-- equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmove rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.NotEqual)
            {
                assembly.Add(";-- not equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovne rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.LessThan)
            {
                assembly.Add(";-- less than --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovl rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.LessThanOrEqual)
            {
                assembly.Add(";-- less than or equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovle rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.GreaterThan)
            {
                assembly.Add(";-- greater than --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovg rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.GreaterThanOrEqual)
            {
                assembly.Add(";-- greater than or equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovge rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else
            {
                throw new Exception($"Unknown operator {op} `{operation.Token.Value}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
        }
        else if (operation.Type is OperationType.Print)
        {
            assembly.Add(";-- print --");
            assembly.Add("  pop rdi");
            assembly.Add("  call print");
        }
        else if (operation.Type is OperationType.PrintString)
        {
            assembly.Add(";-- print string --");
            assembly.Add("  pop rsi");
            assembly.Add("  pop rdx");
            assembly.Add("  mov rdi, 1");
            assembly.Add("  mov rax, 1");
            assembly.Add("  syscall");
        }
        else if (operation.Type is OperationType.JumpIfZero)
        {
            var endLabel = operation.Data?.Text
                ?? throw new Exception("If-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add(";-- jump if zero --");
            assembly.Add($"  pop rax");
            assembly.Add($"  cmp rax, 0");
            assembly.Add($"  jz {endLabel}");
        }
        else if (operation.Type is OperationType.JumpIfNotZero)
        {
            var endLabel = operation.Data?.Text
                ?? throw new Exception("If-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add(";-- jump if not zero --");
            assembly.Add($"  pop rax");
            assembly.Add($"  cmp rax, 0");
            assembly.Add($"  jnz {endLabel}");
        }
        else if (operation.Type is OperationType.Label)
        {
            var endLabel = operation.Data?.Text
                ?? throw new Exception("Block-end-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add($"{endLabel}:");
        }
        else if (operation.Type is OperationType.Jump)
        {
            var label = operation.Data?.Text
                ?? throw new Exception("Jump-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add(";-- jump --");
            assembly.Add($"  jmp {label}");
        }
        else
        {
            throw new Exception($"Unknown operation `{operation.Type}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
        }
    }

    // EXIT syscall
    assembly.Add("  mov rax, 60");
    assembly.Add("  mov rdi, 0");
    assembly.Add("  syscall");

    // NOTE: removed the 'writeable' flag since these are constant string literals
    assembly.Add("segment readable");
    for (var i = 0; i < stringLiterals.Count; i++)
    {
        // TODO: Do this cleaner. This is potentially wasteful (emtpy "" at the end).
        var stringLiteral = stringLiterals[i]
            .Replace("\\r", "\", 13, \"")
            .Replace("\\n", "\", 10, \"")
            .Replace("\\t", "\", 9, \"");
        assembly.Add($"string_{i}: db \"{stringLiteral}\"");
    }

    File.WriteAllLines($"{filename.Split(".")[0]}.asm", assembly);
}

static ParsedProgram ParseProgram(Queue<Token> tokens)
{
    var program = new ParsedProgram(new List<Operation>());

    while (tokens.Count > 0)
    {
        var token = tokens.Dequeue();
        if (int.TryParse(token.Value, out var value))
        {
            program.Operations.Add(new Operation(OperationType.PushNumber, token, new Meta(Number: value)));
        }
        else if (token.Value is "+")
        {
            var nextToken = GetNextToken($"Expected number after +, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after +, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.Add)));
        }
        else if (token.Value is "-")
        {
            var nextToken = GetNextToken($"Expected number after -, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after -, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.Subtract)));
        }
        else if (token.Value is "*")
        {
            var nextToken = GetNextToken($"Expected number after *, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after *, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.Multiply)));
        }
        else if (token.Value is "/")
        {
            var nextToken = GetNextToken($"Expected number after /, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after /, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.Divide)));
        }
        else if (token.Value is "=")
        {
            var nextToken = GetNextToken($"Expected number after =, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after =, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }
            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.Equal)));
        }
        else if (token.Value is "!=")
        {
            var nextToken = GetNextToken($"Expected number after !=, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after !=, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }
            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.NotEqual)));
        }
        else if (token.Value is "<")
        {
            var nextToken = GetNextToken($"Expected number after <, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after <, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }
            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.LessThan)));
        }
        else if (token.Value is "<=")
        {
            var nextToken = GetNextToken($"Expected number after <=, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after <=, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }
            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.LessThanOrEqual)));
        }
        else if (token.Value is ">")
        {
            var nextToken = GetNextToken($"Expected number after >, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after >, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }
            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.GreaterThan)));
        }
        else if (token.Value is ">=")
        {
            var nextToken = GetNextToken($"Expected number after >=, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after >=, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }
            program.Operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: Operator.GreaterThanOrEqual)));
        }
        else if (token.Value is "dup")
        {
            program.Operations.Add(new Operation(OperationType.PushDuplicate, token));
        }
        else if (token.Value is "print")
        {
            program.Operations.Add(new Operation(OperationType.Print, token));
        }
        else if (token.Value is "prints")
        {
            program.Operations.Add(new Operation(OperationType.PrintString, token));
        }
        else if (token.Value.StartsWith('"') && token.Value.EndsWith('"'))
        {
            program.Operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: token.Value[1..^1])));
        }
        else if (token.Value is "yes")
        {
            program.Operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: true)));
        }
        else if (token.Value is "no")
        {
            program.Operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: false)));
        }
        else if (token.Value is "?")
        {
            var nextToken = GetNextToken($"Expected `yes:` or `no:` after ?, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (nextToken.Value is not "yes:" and not "no:")
            {
                throw new Exception($"Expected `yes:` or `no:` after ?, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            var yesBlockTokens = new Queue<Token>();
            var noBlockTokens = new Queue<Token>();
            var invalidBlockTokens = new List<string>
            {
                "yes:",
                "no:",
            };
        other:
            if (nextToken.Value is "yes:")
            {
                var yesToken = nextToken;
                nextToken = GetNextToken($"Unclosed `yes:` block @ {yesToken.Filename}:{yesToken.Line}:{yesToken.Column}");
                while (nextToken.Value is not ";")
                {
                    if (nextToken.Value is "?")
                    {
                        (var block, tokens) = GetIfBlockTokens(new Queue<Token>(tokens.Prepend(nextToken)));
                        foreach (var t in block)
                        {
                            yesBlockTokens.Enqueue(t);
                        }
                    }
                    else if (invalidBlockTokens.Contains(nextToken.Value))
                    {
                        throw new Exception($"Unexpected `{nextToken.Value}` inside `yes:` block @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
                    }
                    else
                    {
                        yesBlockTokens.Enqueue(nextToken);
                    }
                    nextToken = GetNextToken($"Unclosed `yes:` block @ {yesToken.Filename}:{yesToken.Line}:{yesToken.Column}");
                }

                if (yesBlockTokens.Count == 0)
                {
                    throw new Exception($"Empty `yes:` block @ {yesToken.Filename}:{yesToken.Line}:{yesToken.Column}");
                }

                if (tokens.Count > 0)
                {
                    nextToken = tokens.Peek();
                    if (nextToken.Value is "no:")
                    {
                        if (noBlockTokens.Count is not 0)
                        {
                            throw new Exception($"`no:` block is already defined for this `?` statement @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
                        }
                        nextToken = tokens.Dequeue();
                        goto other;
                    }
                }
            }
            else
            {
                var noToken = nextToken;
                nextToken = GetNextToken($"Unclosed `no:` block @ {noToken.Filename}:{noToken.Line}:{noToken.Column}");
                while (nextToken.Value is not ";")
                {
                    if (nextToken.Value is "?")
                    {
                        (var block, tokens) = GetIfBlockTokens(new Queue<Token>(tokens.Prepend(nextToken)));
                        foreach (var t in block)
                        {
                            noBlockTokens.Enqueue(t);
                        }
                    }
                    else if (invalidBlockTokens.Contains(nextToken.Value))
                    {
                        throw new Exception($"Unexpected `{nextToken.Value}` inside `no:` block @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
                    }
                    else
                    {
                        noBlockTokens.Enqueue(nextToken);
                    }
                    nextToken = GetNextToken($"Unclosed `no:` block @ {noToken.Filename}:{noToken.Line}:{noToken.Column}");
                }

                if (noBlockTokens.Count == 0)
                {
                    throw new Exception($"Empty `no:` block @ {noToken.Filename}:{noToken.Line}:{noToken.Column}");
                }

                if (tokens.Count > 0)
                {
                    nextToken = tokens.Peek();
                    if (nextToken.Value is "yes:")
                    {
                        if (yesBlockTokens.Count is not 0)
                        {
                            throw new Exception($"`yes:` block is already defined for this `?` statement @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
                        }
                        nextToken = tokens.Dequeue();
                        goto other;
                    }
                }
            }

            var noBlockProgram = ParseProgram(noBlockTokens);
            var yesBlockProgram = ParseProgram(yesBlockTokens);
            if (noBlockProgram.Operations.Count is 0)
            {
                var endLabel = $"end_{Guid.NewGuid().ToString().Replace("-", "")}";
                program.Operations.Add(new Operation(OperationType.JumpIfZero, token, new Meta(Text: endLabel)));
                program.Operations.AddRange(yesBlockProgram.Operations);
                program.Operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
            }
            else if (yesBlockProgram.Operations.Count is 0)
            {
                var endLabel = $"end_{Guid.NewGuid().ToString().Replace("-", "")}";
                program.Operations.Add(new Operation(OperationType.JumpIfNotZero, token, new Meta(Text: endLabel)));
                program.Operations.AddRange(noBlockProgram.Operations);
                program.Operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
            }
            else
            {
                var yesLabel = $"yes_{Guid.NewGuid().ToString().Replace("-", "")}";
                var endLabel = $"end_{Guid.NewGuid().ToString().Replace("-", "")}";
                program.Operations.Add(new Operation(OperationType.JumpIfNotZero, token, new Meta(Text: yesLabel)));
                program.Operations.AddRange(noBlockProgram.Operations);
                program.Operations.Add(new Operation(OperationType.Jump, token, new Meta(Text: endLabel)));
                program.Operations.Add(new Operation(OperationType.Label, token, new Meta(Text: yesLabel)));
                program.Operations.AddRange(yesBlockProgram.Operations);
                program.Operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
            }
        }
        else
        {
            throw new Exception($"Unknown token `{token.Value}` @ {token.Filename}:{token.Line}:{token.Column}");
        }
    }
    return program;

    Token GetNextToken(string failedMessage)
    {
        if (tokens.Count == 0)
        {
            throw new Exception(failedMessage);
        }
        return tokens.Dequeue();
    }

    (List<Token>, Queue<Token>) GetIfBlockTokens(Queue<Token> tokens)
    {
        var token = tokens.Dequeue();
        if (token.Value is not "?")
        {
            throw new Exception($"Expected `?` @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var blockTokens = new List<Token> { token };

        token = tokens.Dequeue();
        if (token.Value is not "yes:" and not "no:")
        {
            throw new Exception($"Expected `yes:` or `no:` @ {token.Filename}:{token.Line}:{token.Column}");
        }
        blockTokens.Add(token);
        var firstBlockToken = token.Value;

        FindBranchBlockTokens(blockTokens);

        if (tokens.Count is 0)
        {
            return (blockTokens, tokens);
        }

        token = tokens.Peek();
        if (token.Value is "yes:" or "no:" && token.Value != firstBlockToken)
        {
            blockTokens.Add(tokens.Dequeue());
            FindBranchBlockTokens(blockTokens);
        }

        return (blockTokens, tokens);

        void FindBranchBlockTokens(List<Token> blockTokens)
        {
            token = tokens.Dequeue();
            while (token.Value is not ";")
            {
                if (token.Value is "?")
                {
                    (var block, tokens) = GetIfBlockTokens(new Queue<Token>(tokens.Prepend(token)));
                    blockTokens.AddRange(block);
                }
                else
                {
                    blockTokens.Add(token);
                }
                if (tokens.Count is 0)
                {
                    throw new Exception($"Unclosed `?` block @ {token.Filename}:{token.Line}:{token.Column}");
                }
                token = tokens.Dequeue();
            }
            blockTokens.Add(token);
        }
    }
}

static Queue<Token> LexProgram(string filename)
{
    var lines = File.ReadAllLines(filename);
    var words = new Queue<Token>();
    var lineNr = 1;
    foreach (var line in lines)
    {
        var remainingLine = line.TrimStart();
        var currentColumn = line.Length - remainingLine.Length + 1; // index starts at 1
        while (remainingLine.Length > 0)
        {
            if (remainingLine.StartsWith('"'))
            {
                var endQuoteIndex = remainingLine.IndexOf('"', 1);
                if (endQuoteIndex == -1)
                {
                    throw new Exception($"Missing end quote for string literal `{remainingLine}` @ {filename}:{lineNr}:{currentColumn}");
                }
                var stringLiteral = remainingLine[..(endQuoteIndex + 1)];
                words.Enqueue(new Token(filename, stringLiteral, lineNr, currentColumn));
                remainingLine = remainingLine[(endQuoteIndex + 2)..].TrimStart();
            }
            else
            {
                var split = remainingLine.Split(' ', 2);
                words.Enqueue(new Token(filename, split[0], lineNr, currentColumn));
                if (split.Length > 1)
                {
                    currentColumn += split[0].Length + 1; // +1 for the space
                    remainingLine = split[1].TrimStart();
                    currentColumn += split[1].Length - remainingLine.Length;
                }
                else
                {
                    remainingLine = "";
                }
            }
        }
        lineNr++;
    }
    return words;
}

record Token(string Filename, string Value, int Line, int Column);

enum Operator
{
    Add,
    Subtract,
    Multiply,
    Divide,

    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
}

enum OperationType
{
    Print,
    PrintString,
    JumpIfZero,
    JumpIfNotZero,
    Label,
    Jump,
    PushNumber,
    PushString,
    PushBool,
    PushDuplicate,
    Operator,
}

record Meta(int? Number = null, string? Text = null, Operator? Operator = null, bool? Bool = null);

record Operation(OperationType Type, Token Token, Meta? Data = null);

record ParsedProgram(List<Operation> Operations);
